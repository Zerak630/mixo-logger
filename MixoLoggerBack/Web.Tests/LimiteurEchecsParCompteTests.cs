using System.Net;
using System.Net.Http.Json;
using Web.Securite;
using Xunit;

namespace Web.Tests;

/// <summary>Limitation des échecs de connexion par compte, en plus de la limite par adresse IP.</summary>
public class LimiteurEchecsParCompteTests
{
    private sealed class Horloge : TimeProvider
    {
        public DateTimeOffset Maintenant { get; set; } = new(2026, 9, 26, 8, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Maintenant;
    }

    private readonly Horloge _horloge = new();

    private LimiteurEchecsParCompte Limiteur() => new(3, TimeSpan.FromMinutes(15), _horloge);

    [Fact]
    public void BloqueAuDelaDuMaximum_JusquaCeQueLePremierEchecSorteDeLaFenetre()
    {
        var limiteur = Limiteur();

        for (int i = 0; i < 3; i++)
        {
            Assert.False(limiteur.EstBloque("alice", out _));
            limiteur.EnregistrerEchec("alice");
            _horloge.Maintenant = _horloge.Maintenant.AddMinutes(1);
        }

        Assert.True(limiteur.EstBloque("alice", out TimeSpan attente));
        Assert.Equal(TimeSpan.FromMinutes(12), attente);

        _horloge.Maintenant = _horloge.Maintenant.AddMinutes(12);
        Assert.False(limiteur.EstBloque("alice", out _));
    }

    [Fact]
    public void MemeCompte_CasseEtAccentsIgnores_AutresComptesIntacts()
    {
        var limiteur = Limiteur();

        limiteur.EnregistrerEchec("alice");
        limiteur.EnregistrerEchec("ALICE");
        limiteur.EnregistrerEchec(" Àlice ");

        Assert.True(limiteur.EstBloque("Alice", out _));
        Assert.False(limiteur.EstBloque("bob", out _));
    }

    [Fact]
    public void ConnexionReussie_RemetLeCompteurAZero()
    {
        var limiteur = Limiteur();
        limiteur.EnregistrerEchec("alice");
        limiteur.EnregistrerEchec("alice");

        limiteur.Reinitialiser("alice");
        limiteur.EnregistrerEchec("alice");
        limiteur.EnregistrerEchec("alice");

        Assert.False(limiteur.EstBloque("alice", out _));
    }

    [Fact]
    public void IdentifiantVide_JamaisCompte()
    {
        var limiteur = Limiteur();
        for (int i = 0; i < 5; i++)
            limiteur.EnregistrerEchec("  ");

        Assert.False(limiteur.EstBloque("", out _));
    }

    [Fact]
    public void BalayageDIdentifiants_LesEntreesExpireesSontPurgees()
    {
        var limiteur = Limiteur();
        for (int i = 0; i < 1500; i++)
            limiteur.EnregistrerEchec($"inconnu{i}");

        _horloge.Maintenant = _horloge.Maintenant.AddMinutes(16);
        limiteur.EnregistrerEchec("alice");
        limiteur.EnregistrerEchec("alice");
        limiteur.EnregistrerEchec("alice");

        // La purge ne touche que les entrées expirées.
        Assert.True(limiteur.EstBloque("alice", out _));
        Assert.False(limiteur.EstBloque("inconnu0", out _));
    }

    [Fact]
    public async Task Api_TropDEchecsSurUnCompte_429MemeAvecLeBonMotDePasse_SansToucherAuxAutres()
    {
        const string motDePasse = "mot-de-passe-de-test-uniquement";
        using var baseTemporaire = new ApiAvecBaseTemporaire();
        using var api = baseTemporaire.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Comptes:0:Identifiant", "alice");
            builder.UseSetting("Comptes:0:MotDePasse", motDePasse);
            builder.UseSetting("Comptes:1:Identifiant", "bob");
            builder.UseSetting("Comptes:1:MotDePasse", motDePasse);
            // La limite par IP ne doit pas intervenir : c'est celle par compte qu'on vérifie.
            builder.UseSetting("Securite:TentativesDeConnexionParMinute", "1000");
            builder.UseSetting("Securite:EchecsParCompte", "3");
        });
        var client = api.CreateClient();
        Task<HttpResponseMessage> Connexion(string identifiant, string mdp) =>
            client.PostAsJsonAsync("/api/auth/connexion", new { identifiant, motDePasse = mdp }, Http.Jeton);

        for (int i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Connexion(i % 2 == 0 ? "alice" : "ALICE", "faux-mot-de-passe")).StatusCode);

        var bloquee = await Connexion("alice", motDePasse);
        Assert.Equal(HttpStatusCode.TooManyRequests, bloquee.StatusCode);
        string detail = await bloquee.DetailAsync();
        Assert.Equal("Trop de tentatives de connexion sur ce compte. Réessaie dans 15 minutes.", detail);
        Assert.NotNull(bloquee.Headers.RetryAfter);

        // Bob n'est pas touché, et chaque connexion réussie efface ses échecs précédents.
        for (int tour = 0; tour < 2; tour++)
        {
            await Connexion("bob", "faux-mot-de-passe");
            await Connexion("bob", "faux-mot-de-passe");
            Assert.Equal(HttpStatusCode.OK, (await Connexion("bob", motDePasse)).StatusCode);
        }

        // Un identifiant inconnu est traité pareil : le blocage ne révèle pas quels comptes existent.
        for (int i = 0; i < 3; i++)
            await Connexion("personne", "faux-mot-de-passe");
        var inconnu = await Connexion("personne", "faux-mot-de-passe");
        Assert.Equal(HttpStatusCode.TooManyRequests, inconnu.StatusCode);
        Assert.Equal(detail, await inconnu.DetailAsync());
    }
}
