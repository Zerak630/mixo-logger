using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Web.Tests.Http;

namespace Web.Tests;

/// <summary>Sécurité de l'API au-delà de la connexion elle-même (cf. AuthentificationTests).</summary>
public class SecuriteApiTests(ApiAvecComptes api) : IClassFixture<ApiAvecComptes>
{
    public static TheoryData<string, string> RoutesProtegees => new()
    {
        { "PUT", $"/api/cocktails/{Guid.Empty}/note" },
        { "DELETE", $"/api/cocktails/{Guid.Empty}/note" },
        { "DELETE", $"/api/cocktails/{Guid.Empty}" },
        { "PUT", $"/api/cocktails/{Guid.Empty}" },
        { "POST", "/api/cocktails" },
        { "POST", "/api/bars/MakeCocktails" },
        { "PATCH", "/api/bars/ingredients/Gin" },
        { "DELETE", "/api/bars/ingredients/Gin" },
        { "GET", $"/api/cocktails/{Guid.Empty}" },
    };

    [Theory]
    [MemberData(nameof(RoutesProtegees))]
    public async Task SansSession_ToutesLesRoutesEnEcriture_401(string verbe, string url)
    {
        using var requete = new HttpRequestMessage(new HttpMethod(verbe), url) { Content = JsonContent.Create(new { }) };

        var reponse = await api.CreateClient().SendAsync(requete, Jeton);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Theory]
    [InlineData("mixo_session=nimportequoi")]
    [InlineData("mixo_session=")]
    public async Task CookieFalsifie_401(string cookie)
    {
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var requete = new HttpRequestMessage(HttpMethod.Get, "/api/auth/moi");
        requete.Headers.Add("Cookie", cookie);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(requete, Jeton)).StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"identifiant":null,"motDePasse":null}""")]
    [InlineData("""{"identifiant":"alice"}""")]
    [InlineData("""{"identifiant":"alice","motDePasse":""}""")]
    public async Task Connexion_ChampsManquants_401_PasDErreurServeur(string corps)
    {
        var reponse = await api.CreateClient().PostAsync("/api/auth/connexion", new StringContent(corps, System.Text.Encoding.UTF8, "application/json"), Jeton);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task CorpsJsonMalForme_400()
    {
        var alice = await api.ConnecterAsync("alice");

        var reponse = await alice.PostAsync("/api/cocktails", new StringContent("{ pas du json", System.Text.Encoding.UTF8, "application/json"), Jeton);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task IdentiteGlisseeDansLeCorps_EstIgnoree()
    {
        var alice = await api.ConnecterAsync("alice");
        var bob = await api.ConnecterAsync("bob");
        string idBob = (await bob.GetJsonAsync("/api/auth/moi")).GetProperty("id").GetString()!;
        string ingredient = Unique("Tentative");

        // Alice tente d'écrire dans le bar de Bob et de créer une recette à son nom.
        await alice.PostAsJsonAsync("/api/bars/ingredients", new { name = ingredient, ownerId = idBob, proprietaireId = idBob }, Jeton);
        var creation = await alice.PostAsJsonAsync("/api/cocktails", new
        {
            name = Unique("Usurpée"),
            authorId = idBob,
            auteurId = idBob,
            ingredients = new[] { new { name = "Gin", valeur = 4, unite = "cL" } },
            etapes = new[] { "Verser" }
        }, Jeton);

        Assert.True((await alice.BarAsync()).ContainsKey(ingredient));
        Assert.False((await bob.BarAsync()).ContainsKey(ingredient));

        string id = (await creation.JsonAsync()).GetProperty("id").GetString()!;
        Assert.True((await alice.GetJsonAsync($"/api/cocktails/{id}")).GetProperty("modifiable").GetBoolean());
        Assert.False((await bob.GetJsonAsync($"/api/cocktails/{id}")).GetProperty("modifiable").GetBoolean());
    }

    [Fact]
    public async Task ActionNonAutorisee_403_EnProblemDetails_SansDetailTechnique()
    {
        var alice = await api.ConnecterAsync("alice");
        var bob = await api.ConnecterAsync("bob");
        string id = await alice.CreerRecetteAsync(Unique("Protégée"), ("Gin", 4, "cL"));

        var reponse = await bob.DeleteAsync($"/api/cocktails/{id}", Jeton);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
        string detail = await reponse.DetailAsync();
        Assert.Contains("Seul l'auteur", detail);
        Assert.DoesNotContain("Exception", detail);
    }

    [Fact]
    public async Task ErreurInattendue_NeDivulguePasDeTraceDePile()
    {
        var alice = await api.ConnecterAsync("alice");

        // Identifiant de recette inexistant dans une commande : erreur métier, pas d'exception brute.
        var reponse = await alice.PostAsJsonAsync("/api/bars/MakeCocktails", new[] { new { cocktailId = Guid.NewGuid(), quantity = 1 } }, Jeton);
        string corps = await reponse.Content.ReadAsStringAsync(Jeton);

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        Assert.DoesNotContain("   at ", corps);
        Assert.DoesNotContain("StackTrace", corps, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Un compte retiré de la configuration perd sa session à la requête suivante, même si son cookie
/// est encore valide. On démarre deux fois l'API avec les mêmes clés de chiffrement des cookies :
/// une fois avec le compte, une fois sans.
/// </summary>
public class CompteRetireTests : IDisposable
{
    private readonly string _cles = Path.Combine(Path.GetTempPath(), $"mixologger-cles-{Guid.NewGuid():N}");

    private WebApplicationFactory<Program> Api(ApiAvecBaseTemporaire baseDeDonnees, params string[] comptes) =>
        baseDeDonnees.WithWebHostBuilder(builder =>
        {
            for (int i = 0; i < comptes.Length; i++)
            {
                builder.UseSetting($"Comptes:{i}:Identifiant", comptes[i]);
                builder.UseSetting($"Comptes:{i}:MotDePasse", ApiAvecComptes.MotDePasse);
            }
            builder.ConfigureTestServices(services => services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(_cles))
                .SetApplicationName("MixoLogger-tests"));
        });

    private static async Task<string> CookieDeSessionAsync(WebApplicationFactory<Program> api)
    {
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var reponse = await client.PostAsJsonAsync("/api/auth/connexion", new { identifiant = "alice", motDePasse = ApiAvecComptes.MotDePasse }, Jeton);
        reponse.EnsureSuccessStatusCode();
        return reponse.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("mixo_session=")).Split(';')[0];
    }

    private static async Task<HttpStatusCode> MoiAsync(WebApplicationFactory<Program> api, string cookie)
    {
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var requete = new HttpRequestMessage(HttpMethod.Get, "/api/auth/moi");
        requete.Headers.Add("Cookie", cookie);
        return (await client.SendAsync(requete, Jeton)).StatusCode;
    }

    [Fact]
    public async Task CompteRetire_SessionRejetee_CompteConserve_SessionAcceptee()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();

        string cookie;
        await using (var avecAlice = Api(baseDeDonnees, "alice", "bob"))
            cookie = await CookieDeSessionAsync(avecAlice);

        // Témoin : le cookie reste valable pour une autre instance qui connaît toujours alice…
        await using (var toujoursAlice = Api(baseDeDonnees, "alice"))
            Assert.Equal(HttpStatusCode.OK, await MoiAsync(toujoursAlice, cookie));

        // … mais plus pour celle où alice a été retirée.
        await using (var sansAlice = Api(baseDeDonnees, "bob"))
            Assert.Equal(HttpStatusCode.Unauthorized, await MoiAsync(sansAlice, cookie));
    }

    public void Dispose()
    {
        try { Directory.Delete(_cles, recursive: true); } catch (IOException) { }
        GC.SuppressFinalize(this);
    }
}
