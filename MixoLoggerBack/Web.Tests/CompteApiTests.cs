using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Web.Tests.Http;

namespace Web.Tests;

/// <summary>
/// Le compte de l'utilisateur connecté : changer son mot de passe (les autres sessions se ferment),
/// changer d'identifiant sans rien perdre. Les comptes sont en base : ces changements survivent au
/// redémarrage, et la configuration ne les écrase pas.
/// </summary>
public class CompteApiTests
{
    private const string MotDePasse = ApiAvecComptes.MotDePasse;
    private const string NouveauMotDePasse = "un-nouveau-mot-de-passe";

    /// <summary>Une API par test, sur <paramref name="baseDeDonnees"/> : les changements de mot de passe ne gênent pas les autres tests.</summary>
    private static WebApplicationFactory<Program> Demarrer(ApiAvecBaseTemporaire baseDeDonnees, int echecsParCompte = 1000) =>
        baseDeDonnees.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Comptes:0:Identifiant", "alice");
            builder.UseSetting("Comptes:0:NomAffiche", "Alice");
            builder.UseSetting("Comptes:0:MotDePasse", MotDePasse);
            builder.UseSetting("Comptes:1:Identifiant", "bob");
            builder.UseSetting("Comptes:1:MotDePasse", MotDePasse);
            builder.UseSetting("Securite:TentativesDeConnexionParMinute", "1000");
            builder.UseSetting("Securite:EchecsParCompte", echecsParCompte.ToString());
        });

    private static Task<HttpResponseMessage> ConnexionAsync(WebApplicationFactory<Program> api, string identifiant, string motDePasse) =>
        api.CreateClient().PostAsJsonAsync("/api/auth/connexion", new { identifiant, motDePasse }, Jeton);

    private static async Task<HttpClient> ConnecterAsync(WebApplicationFactory<Program> api, string identifiant, string motDePasse = MotDePasse)
    {
        HttpClient client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/connexion", new { identifiant, motDePasse }, Jeton)).EnsureSuccessStatusCode();
        return client;
    }

    private static Task<HttpResponseMessage> ChangerMotDePasseAsync(HttpClient client, string actuel, string nouveau) =>
        client.PutAsJsonAsync("/api/compte/mot-de-passe", new { actuel, nouveau }, Jeton);

    [Fact]
    public async Task ChangerSonMotDePasse_LAncienNeMarchePlus_LesAutresSessionsSontFermees_PasCelleCi()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var ici = await ConnecterAsync(api, "alice");
        var ailleurs = await ConnecterAsync(api, "alice");
        var bob = await ConnecterAsync(api, "bob");

        Assert.Equal(HttpStatusCode.NoContent, (await ChangerMotDePasseAsync(ici, MotDePasse, NouveauMotDePasse)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await ici.GetAsync("/api/auth/moi", Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ailleurs.GetAsync("/api/auth/moi", Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync("/api/auth/moi", Jeton)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await ConnexionAsync(api, "alice", MotDePasse)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ConnexionAsync(api, "alice", NouveauMotDePasse)).StatusCode);
    }

    [Theory]
    [InlineData("pas-le-bon-mot-de-passe", NouveauMotDePasse, "Le mot de passe actuel est incorrect.")]
    [InlineData(MotDePasse, "court", "Le mot de passe doit faire au moins 12 caractères.")]
    [InlineData(MotDePasse, MotDePasse, "Le nouveau mot de passe doit être différent de l'actuel.")]
    public async Task ChangementRefuse_400_EtRienNeChange(string actuel, string nouveau, string message)
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var alice = await ConnecterAsync(api, "alice");

        var reponse = await ChangerMotDePasseAsync(alice, actuel, nouveau);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal(message, await reponse.DetailAsync());
        // 400 et non 401 : la session reste ouverte, et le mot de passe n'a pas changé.
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync("/api/auth/moi", Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ConnexionAsync(api, "alice", MotDePasse)).StatusCode);
    }

    [Fact]
    public async Task MotDePasseActuel_TropDEssais_429()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees, echecsParCompte: 3);
        var alice = await ConnecterAsync(api, "alice");

        for (int i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await ChangerMotDePasseAsync(alice, "pas-le-bon-mot-de-passe", NouveauMotDePasse)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await ChangerMotDePasseAsync(alice, MotDePasse, NouveauMotDePasse)).StatusCode);
    }

    [Fact]
    public async Task ChangerDIdentifiant_GardeRecettesEtBar_EtLaSessionSuit()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var alice = await ConnecterAsync(api, "alice");
        string id = await alice.CreerRecetteAsync(Unique("Recette d'Alice"), ("Gin", 4, "cL"));
        (await alice.PostAsJsonAsync("/api/bars/ingredients", new { name = "Gin" }, Jeton)).EnsureSuccessStatusCode();

        var reponse = await alice.PutAsJsonAsync("/api/compte", new { identifiant = " Alicia ", nomAffiche = "Alicia L." }, Jeton);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        JsonElement moi = await alice.GetJsonAsync("/api/auth/moi");
        Assert.Equal("Alicia", moi.GetProperty("identifiant").GetString());
        Assert.Equal("Alicia L.", moi.GetProperty("nomAffiche").GetString());

        // Même compte : la recette est toujours la sienne, sous son nouveau nom, et son bar est intact.
        JsonElement recette = await alice.GetJsonAsync($"/api/cocktails/{id}");
        Assert.True(recette.GetProperty("modifiable").GetBoolean());
        Assert.Equal("Alicia L.", recette.GetProperty("auteur").GetString());
        Assert.Contains("Gin", (await alice.BarAsync()).Keys);

        Assert.Equal(HttpStatusCode.Unauthorized, (await ConnexionAsync(api, "alice", MotDePasse)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ConnexionAsync(api, "ALICIA", MotDePasse)).StatusCode);
    }

    [Theory]
    [InlineData("BOB", HttpStatusCode.Conflict, "L'identifiant « BOB » est déjà pris.")]
    [InlineData("  ", HttpStatusCode.BadRequest, "L'identifiant est obligatoire.")]
    public async Task ChangerDIdentifiant_Refuse_EtRienNeChange(string identifiant, HttpStatusCode statut, string message)
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var alice = await ConnecterAsync(api, "alice");

        var reponse = await alice.PutAsJsonAsync("/api/compte", new { identifiant, nomAffiche = "X" }, Jeton);

        Assert.Equal(statut, reponse.StatusCode);
        Assert.Equal(message, await reponse.DetailAsync());
        Assert.Equal("Alice", (await alice.GetJsonAsync("/api/auth/moi")).GetProperty("nomAffiche").GetString());
    }

    [Fact]
    public async Task Redemarrage_LesChangementsTiennent_LaConfigurationNeLesEcrasePas()
    {
        string fichier = ApiAvecBaseTemporaire.NouveauFichier();

        try
        {
            await using (var baseDeDonnees = new ApiAvecBaseTemporaire(fichier))
            await using (var api = Demarrer(baseDeDonnees))
            {
                var alice = await ConnecterAsync(api, "alice");
                Assert.Equal(HttpStatusCode.NoContent, (await ChangerMotDePasseAsync(alice, MotDePasse, NouveauMotDePasse)).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await alice.PutAsJsonAsync("/api/compte", new { identifiant = "alicia", nomAffiche = "Alicia" }, Jeton)).StatusCode);
            }

            // La configuration déclare toujours « alice » avec l'ancien mot de passe.
            await using (var baseDeDonnees = new ApiAvecBaseTemporaire(fichier))
            await using (var api = Demarrer(baseDeDonnees))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await ConnexionAsync(api, "alice", MotDePasse)).StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, (await ConnexionAsync(api, "alicia", MotDePasse)).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await ConnexionAsync(api, "alicia", NouveauMotDePasse)).StatusCode);
            }
        }
        finally
        {
            ApiAvecBaseTemporaire.SupprimerBase(fichier);
        }
    }

    [Fact]
    public async Task MotDePasseOublie_ReinitialiseParLaConfiguration()
    {
        string fichier = ApiAvecBaseTemporaire.NouveauFichier();

        try
        {
            await using (var baseDeDonnees = new ApiAvecBaseTemporaire(fichier))
            await using (var api = Demarrer(baseDeDonnees))
            {
                var alice = await ConnecterAsync(api, "alice");
                await ChangerMotDePasseAsync(alice, MotDePasse, NouveauMotDePasse);
            }

            await using (var baseDeDonnees = new ApiAvecBaseTemporaire(fichier))
            await using (var api = Demarrer(baseDeDonnees).WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Comptes:0:MotDePasse", "remis-par-l-administrateur");
                builder.UseSetting("Comptes:0:ReinitialiserMotDePasse", "true");
            }))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, (await ConnexionAsync(api, "alice", NouveauMotDePasse)).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await ConnexionAsync(api, "alice", "remis-par-l-administrateur")).StatusCode);
            }
        }
        finally
        {
            ApiAvecBaseTemporaire.SupprimerBase(fichier);
        }
    }

    [Fact]
    public async Task SansSession_401()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var client = api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/compte", Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ChangerMotDePasseAsync(client, MotDePasse, NouveauMotDePasse)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/compte", new { identifiant = "x" }, Jeton)).StatusCode);
    }
}
