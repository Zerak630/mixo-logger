using Domain.Interfaces.Repositories;
using Domain.Utilisateurs;
using Infrastructure.Securite;
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

/// <summary>
/// Comptes en base, créés et tenus à jour depuis la configuration au démarrage ; ensuite modifiables
/// par leur titulaire (identifiant, nom, mot de passe) sans rien perdre.
/// </summary>
public class ComptesTests : BaseDeTest
{
    private const string MotDePasse = "mot-de-passe-long";
    private readonly HacheurMotDePasse _hacheur = new();

    private static CompteConfigure Compte(string identifiant, string? nomAffiche = null, string? motDePasse = MotDePasse, bool reinitialiser = false) =>
        new() { Identifiant = identifiant, NomAffiche = nomAffiche, MotDePasse = motDePasse, ReinitialiserMotDePasse = reinitialiser };

    private Task<BilanComptes> SynchroniserAsync(params CompteConfigure[] comptes) =>
        Services.SynchroniserComptesAsync(comptes, _hacheur, Jeton);

    private Task<Utilisateur?> ParIdentifiantAsync(string identifiant) =>
        AvecAsync<IUtilisateurRepository, Utilisateur?>(depot => depot.GetByIdentifiantAsync(identifiant));

    private Task<Utilisateur?> ParIdAsync(Guid id) =>
        AvecAsync<IUtilisateurRepository, Utilisateur?>(depot => depot.GetByIdAsync(id));

    private Task MettreAJourAsync(Utilisateur utilisateur) =>
        AvecAsync<IUtilisateurRepository>(depot => depot.MettreAJourAsync(utilisateur));

    [Fact]
    public async Task Creation_MotDePasseHache_NomParDefaut_IdDeriveDeLIdentifiant()
    {
        BilanComptes bilan = await SynchroniserAsync(Compte("Élodie", "Élodie M."), Compte("carole"));

        Assert.Equal(new BilanComptes(Crees: 2, Reactives: 0, Reinitialises: 0, Desactives: 0, Actifs: 2), bilan);
        Utilisateur elodie = (await ParIdentifiantAsync("  ELODIE "))!;
        Assert.Equal("Élodie M.", elodie.NomAffiche);
        Assert.NotEqual(MotDePasse, elodie.EmpreinteMotDePasse);
        Assert.True(_hacheur.Verifier(elodie.EmpreinteMotDePasse, MotDePasse));
        Assert.Equal("carole", (await ParIdentifiantAsync("carole"))!.NomAffiche);
        // Même Id qu'avant la mise en base des comptes : bars, recettes et notes existants retrouvent leur titulaire.
        Assert.Equal(new Utilisateur("Élodie", "", "x").Id, elodie.Id);
    }

    [Fact]
    public async Task Recherche_ParId_EtIdentifiantInconnu()
    {
        await SynchroniserAsync(Compte("alice"));
        Utilisateur alice = (await ParIdentifiantAsync("alice"))!;

        Assert.Equal("alice", (await ParIdAsync(alice.Id))?.Identifiant);
        Assert.Null(await ParIdAsync(Guid.NewGuid()));
        Assert.Null(await ParIdentifiantAsync("personne"));
        Assert.Null(await ParIdentifiantAsync(null!));
    }

    [Fact]
    public async Task SecondDemarrage_RienNeChange_EtLeMotDePasseDeConfigurationEstIgnore()
    {
        await SynchroniserAsync(Compte("alice"));
        Utilisateur avant = (await ParIdentifiantAsync("alice"))!;

        // Mot de passe changé, puis retiré de la configuration : il n'est lu qu'à la création.
        Assert.Equal(new BilanComptes(0, 0, 0, 0, 1), await SynchroniserAsync(Compte("alice", motDePasse: "un-tout-autre-mot-de-passe")));
        Assert.Equal(new BilanComptes(0, 0, 0, 0, 1), await SynchroniserAsync(Compte("alice", motDePasse: null)));

        Utilisateur apres = (await ParIdentifiantAsync("alice"))!;
        Assert.Equal(avant.EmpreinteMotDePasse, apres.EmpreinteMotDePasse);
        Assert.Equal(avant.TamponSecurite, apres.TamponSecurite);
    }

    [Fact]
    public async Task EntreeRetiree_CompteDesactive_PuisRemise_CompteReactiveAvecLeMemeId()
    {
        await SynchroniserAsync(Compte("alice"), Compte("bob"));
        Guid id = (await ParIdentifiantAsync("alice"))!.Id;

        Assert.Equal(new BilanComptes(0, 0, 0, Desactives: 1, Actifs: 1), await SynchroniserAsync(Compte("bob")));
        Assert.Null(await ParIdentifiantAsync("alice"));
        Assert.Null(await ParIdAsync(id));

        Assert.Equal(new BilanComptes(0, Reactives: 1, 0, 0, Actifs: 2), await SynchroniserAsync(Compte("alice", motDePasse: null), Compte("bob")));
        Assert.Equal(id, (await ParIdentifiantAsync("alice"))?.Id);
    }

    [Fact]
    public async Task Reinitialisation_NouveauMotDePasse_NouveauTampon()
    {
        await SynchroniserAsync(Compte("alice"));
        Utilisateur avant = (await ParIdentifiantAsync("alice"))!;

        BilanComptes bilan = await SynchroniserAsync(Compte("alice", motDePasse: "nouveau-mot-de-passe", reinitialiser: true));

        Utilisateur apres = (await ParIdentifiantAsync("alice"))!;
        Assert.Equal(1, bilan.Reinitialises);
        Assert.True(_hacheur.Verifier(apres.EmpreinteMotDePasse, "nouveau-mot-de-passe"));
        Assert.False(_hacheur.Verifier(apres.EmpreinteMotDePasse, MotDePasse));
        Assert.NotEqual(avant.TamponSecurite, apres.TamponSecurite);
        Assert.Equal(avant.Id, apres.Id);
    }

    [Fact]
    public async Task Renommage_GardeLId_EtLaConfigurationRetrouveLeCompteParSonIdentifiantDOrigine()
    {
        await SynchroniserAsync(Compte("alice"));
        Utilisateur alice = (await ParIdentifiantAsync("alice"))!;

        await MettreAJourAsync(alice.Renommer("Alicia", "Alicia L."));

        Assert.Null(await ParIdentifiantAsync("alice"));
        Utilisateur alicia = (await ParIdentifiantAsync("alicia"))!;
        Assert.Equal(alice.Id, alicia.Id);
        Assert.Equal("Alicia L.", alicia.NomAffiche);

        // L'entrée « alice » désigne toujours ce compte : rien n'est créé, rien n'est désactivé.
        Assert.Equal(new BilanComptes(0, 0, 0, 0, 1), await SynchroniserAsync(Compte("alice")));
        Assert.Equal(alice.Id, (await ParIdentifiantAsync("alicia"))?.Id);
    }

    [Fact]
    public async Task NouvelleEntree_SurUnIdentifiantPrisParRenommage_EmpecheLeDemarrage()
    {
        await SynchroniserAsync(Compte("alice"));
        await MettreAJourAsync((await ParIdentifiantAsync("alice"))!.Renommer("bob", ""));

        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(() => SynchroniserAsync(Compte("alice"), Compte("BOB")));

        Assert.Contains("Comptes:1", erreur.Message);
        Assert.Contains("déjà utilisé par un autre compte", erreur.Message);
    }

    [Fact]
    public async Task MiseAJour_IdentifiantDejaPris_409()
    {
        await SynchroniserAsync(Compte("alice"), Compte("bob"));
        Utilisateur alice = (await ParIdentifiantAsync("alice"))!;

        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(() => MettreAJourAsync(alice.Renommer(" BOB ", "")));

        Assert.Equal("L'identifiant « BOB » est déjà pris.", erreur.Message);
        Assert.NotNull(await ParIdentifiantAsync("alice"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    public async Task IdentifiantVide_EmpecheLeDemarrage_SansRienEnregistrer(string identifiant)
    {
        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(() => SynchroniserAsync(Compte("alice"), Compte(identifiant)));

        Assert.Contains("Comptes:1", erreur.Message);
        Assert.Contains("identifiant est obligatoire", erreur.Message);
        Assert.Null(await ParIdentifiantAsync("alice"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("onze-carac.")]
    public async Task NouveauCompte_MotDePasseTropCourt_EmpecheLeDemarrage(string? motDePasse)
    {
        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(() => SynchroniserAsync(Compte("alice", motDePasse: motDePasse)));

        Assert.Contains("au moins 12 caractères", erreur.Message);
    }

    [Fact]
    public async Task Reinitialisation_MotDePasseTropCourt_EmpecheLeDemarrage()
    {
        await SynchroniserAsync(Compte("alice"));

        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(() => SynchroniserAsync(Compte("alice", motDePasse: "court", reinitialiser: true)));

        Assert.Contains("au moins 12 caractères", erreur.Message);
    }

    [Fact]
    public async Task MotDePasseDeDouzeCaracteres_EstAccepte()
    {
        Assert.Equal(1, (await SynchroniserAsync(Compte("alice", motDePasse: "douze-caract"))).Crees);
    }

    [Fact]
    public async Task IdentifiantEnDouble_AccentsEtCasseIgnores_EmpecheLeDemarrage()
    {
        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(() => SynchroniserAsync(Compte("Zoé"), Compte("ZOE")));

        Assert.Contains("déclaré plusieurs fois", erreur.Message);
    }

    [Fact]
    public async Task AucunCompte_AucunActif()
    {
        Assert.Equal(new BilanComptes(0, 0, 0, 0, 0), await Services.SynchroniserComptesAsync(null!, _hacheur, Jeton));
    }
}
