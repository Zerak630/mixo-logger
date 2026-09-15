using Domain.Utilisateurs;
using Infrastructure.Repositories;
using Infrastructure.Securite;
using Microsoft.Extensions.Options;
using Xunit;

namespace Infrastructure.Tests;

public class HacheurMotDePasseTests
{
    private readonly HacheurMotDePasse _hacheur = new();

    [Fact]
    public void Empreinte_NeContientPasLeMotDePasse_EtLeVerifie()
    {
        const string motDePasse = "un-mot-de-passe-solide";

        string empreinte = _hacheur.Hacher(motDePasse);

        Assert.DoesNotContain(motDePasse, empreinte);
        Assert.True(_hacheur.Verifier(empreinte, motDePasse));
    }

    [Theory]
    [InlineData("un-mot-de-passe-SOLIDE")]
    [InlineData("un-mot-de-passe-solide ")]
    [InlineData("")]
    public void MotDePasseDifferent_EstRefuse(string essai)
    {
        Assert.False(_hacheur.Verifier(_hacheur.Hacher("un-mot-de-passe-solide"), essai));
    }

    [Fact]
    public void DeuxEmpreintesDuMemeMotDePasse_Different()
    {
        // Sel aléatoire : deux comptes au même mot de passe n'ont pas la même empreinte.
        Assert.NotEqual(_hacheur.Hacher("identique-identique"), _hacheur.Hacher("identique-identique"));
    }

    [Fact]
    public void EmpreinteVide_OuMotDePasseNul_EstRefuse()
    {
        Assert.False(_hacheur.Verifier("", "quelque-chose"));
        Assert.False(_hacheur.Verifier(null!, "quelque-chose"));
        Assert.False(_hacheur.Verifier(_hacheur.Hacher("quelque-chose"), null!));
    }
}

public class UtilisateurRepositoryTests
{
    private const string MotDePasse = "mot-de-passe-long";

    private static UtilisateurRepository Depot(params CompteConfigure[] comptes) =>
        new(Options.Create(new ComptesOptions { Liste = [.. comptes] }), new HacheurMotDePasse());

    private static CompteConfigure Compte(string identifiant, string? nomAffiche = null, string motDePasse = MotDePasse) =>
        new() { Identifiant = identifiant, NomAffiche = nomAffiche, MotDePasse = motDePasse };

    [Fact]
    public async Task Recherche_ParIdentifiant_SansCasseNiAccentsNiEspaces()
    {
        var depot = Depot(Compte("Élodie", "Élodie M."), Compte("bob"));

        Utilisateur? trouve = await depot.GetByIdentifiantAsync("  ELODIE ");

        Assert.NotNull(trouve);
        Assert.Equal("Élodie M.", trouve.NomAffiche);
        Assert.Equal(2, depot.Nombre);
    }

    [Fact]
    public async Task Recherche_ParId_EtIdentifiantInconnu()
    {
        var depot = Depot(Compte("alice"));
        Utilisateur alice = (await depot.GetByIdentifiantAsync("alice"))!;

        Assert.Same(alice, await depot.GetByIdAsync(alice.Id));
        Assert.Null(await depot.GetByIdAsync(Guid.NewGuid()));
        Assert.Null(await depot.GetByIdentifiantAsync("personne"));
        Assert.Null(await depot.GetByIdentifiantAsync(null!));
    }

    [Fact]
    public async Task MotDePasse_EstHache_JamaisConserveEnClair()
    {
        Utilisateur alice = (await Depot(Compte("alice")).GetByIdentifiantAsync("alice"))!;

        Assert.NotEqual(MotDePasse, alice.EmpreinteMotDePasse);
        Assert.True(new HacheurMotDePasse().Verifier(alice.EmpreinteMotDePasse, MotDePasse));
    }

    [Fact]
    public async Task SansNomAffiche_LIdentifiantSertDeNom()
    {
        Assert.Equal("carole", (await Depot(Compte("carole")).GetByIdentifiantAsync("carole"))!.NomAffiche);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    public void IdentifiantVide_EmpecheLaConstruction(string identifiant)
    {
        var erreur = Assert.Throws<InvalidOperationException>(() => Depot(Compte("alice"), Compte(identifiant)));

        Assert.Contains("Comptes:1", erreur.Message);
        Assert.Contains("identifiant est obligatoire", erreur.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("onze-carac.")]
    public void MotDePasseTropCourt_EmpecheLaConstruction(string motDePasse)
    {
        var erreur = Assert.Throws<InvalidOperationException>(() => Depot(Compte("alice", motDePasse: motDePasse)));

        Assert.Contains("au moins 12 caractères", erreur.Message);
    }

    [Fact]
    public void MotDePasseDeDouzeCaracteres_EstAccepte()
    {
        Assert.Equal(1, Depot(Compte("alice", motDePasse: "douze-caract")).Nombre);
    }

    [Fact]
    public void IdentifiantEnDouble_AccentsEtCasseIgnores_EmpecheLaConstruction()
    {
        var erreur = Assert.Throws<InvalidOperationException>(() => Depot(Compte("Zoé"), Compte("ZOE")));

        Assert.Contains("déclaré plusieurs fois", erreur.Message);
    }

    [Fact]
    public void AucunCompte_DepotVide()
    {
        Assert.Equal(0, new UtilisateurRepository(Options.Create(new ComptesOptions { Liste = null! }), new HacheurMotDePasse()).Nombre);
    }
}
