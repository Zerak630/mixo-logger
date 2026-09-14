using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using static Web.Tests.Annulation;

namespace Web.Tests;

/// <summary>Jeton d'annulation du test en cours : un test interrompu n'attend pas la fin de ses requêtes.</summary>
file static class Annulation
{
    public static CancellationToken Jeton => TestContext.Current.CancellationToken;
}

/// <summary>
/// Authentification de bout en bout (F6) : l'API tourne en mémoire avec un compte de test
/// déclaré ci-dessous. Ce compte n'existe que dans les tests.
/// </summary>
public class AuthentificationTests : IClassFixture<AuthentificationTests.ApiDeTest>
{
    private const string Identifiant = "alice";
    private const string MotDePasse = "mot-de-passe-de-test-uniquement";

    public class ApiDeTest : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("Comptes:0:Identifiant", Identifiant);
            builder.UseSetting("Comptes:0:NomAffiche", "Alice");
            builder.UseSetting("Comptes:0:MotDePasse", MotDePasse);
            // Assez haut pour que les autres tests ne se gênent pas ; la limite a son propre test.
            builder.UseSetting("Securite:TentativesDeConnexionParMinute", "1000");
        }
    }

    private readonly ApiDeTest _api;

    public AuthentificationTests(ApiDeTest api) => _api = api;

    private HttpClient NouveauClient() => _api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static Task<HttpResponseMessage> SeConnecter(HttpClient client, string identifiant = Identifiant, string motDePasse = MotDePasse) =>
        client.PostAsJsonAsync("/api/auth/connexion", new { identifiant, motDePasse }, Jeton);

    [Fact]
    public async Task SansSession_LApiRepond401()
    {
        var reponse = await NouveauClient().GetAsync("/api/cocktails", Jeton);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
        Assert.Equal("application/problem+json", reponse.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/api/bars")]
    [InlineData("/api/ingredients")]
    [InlineData("/api/cocktails/unites")]
    [InlineData("/api/auth/moi")]
    public async Task SansSession_ToutEstProtegeParDefaut(string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await NouveauClient().GetAsync(url, Jeton)).StatusCode);
    }

    [Fact]
    public async Task SansSession_LEcritureEstAussiRefusee()
    {
        var reponse = await NouveauClient().PostAsJsonAsync("/api/bars/ingredients", new { name = "Gin" }, Jeton);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Ping_RestePublic()
    {
        Assert.Equal(HttpStatusCode.OK, (await NouveauClient().GetAsync("/ping", Jeton)).StatusCode);
    }

    [Fact]
    public async Task MauvaisMotDePasse_EtIdentifiantInconnu_DonnentLaMemeReponse()
    {
        var client = NouveauClient();

        var mauvaisMotDePasse = await SeConnecter(client, motDePasse: "pas-le-bon-mot-de-passe");
        var identifiantInconnu = await SeConnecter(client, identifiant: "personne");

        Assert.Equal(HttpStatusCode.Unauthorized, mauvaisMotDePasse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, identifiantInconnu.StatusCode);
        // Aucun indice sur l'existence du compte.
        Assert.Equal(await Detail(mauvaisMotDePasse), await Detail(identifiantInconnu));
        Assert.False(mauvaisMotDePasse.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Connexion_PoseUnCookieHttpOnlySameSiteStrict()
    {
        var reponse = await SeConnecter(NouveauClient());

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

        string cookie = Assert.Single(reponse.Headers.GetValues("Set-Cookie"), valeur => valeur.StartsWith("mixo_session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Connexion_RenvoieLUtilisateur_SansEmpreinteDuMotDePasse()
    {
        var reponse = await SeConnecter(NouveauClient());
        string corps = await reponse.Content.ReadAsStringAsync(Jeton);

        Assert.Contains("\"identifiant\":\"alice\"", corps);
        Assert.Contains("\"nomAffiche\":\"Alice\"", corps);
        Assert.DoesNotContain("empreinte", corps, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(MotDePasse, corps);
    }

    [Fact]
    public async Task AvecSession_LApiRepond_EtMoiRenvoieLUtilisateur()
    {
        var client = NouveauClient();
        await SeConnecter(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/cocktails", Jeton)).StatusCode);

        var moi = await client.GetFromJsonAsync<Dictionary<string, object>>("/api/auth/moi", Jeton);
        Assert.Equal("alice", moi!["identifiant"].ToString());
        Assert.Equal("Alice", moi["nomAffiche"].ToString());
    }

    [Fact]
    public async Task Identifiant_InsensibleALaCasseEtAuxEspaces()
    {
        Assert.Equal(HttpStatusCode.OK, (await SeConnecter(NouveauClient(), identifiant: "  ALICE ")).StatusCode);
    }

    [Fact]
    public async Task IdAttribue_EstStableDUneConnexionALAutre()
    {
        var premier = await (await SeConnecter(NouveauClient())).Content.ReadFromJsonAsync<Dictionary<string, object>>(Jeton);
        var second = await (await SeConnecter(NouveauClient())).Content.ReadFromJsonAsync<Dictionary<string, object>>(Jeton);

        Assert.Equal(premier!["id"].ToString(), second!["id"].ToString());
    }

    [Fact]
    public async Task Deconnexion_FermeLaSession()
    {
        var client = NouveauClient();
        await SeConnecter(client);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/deconnexion", null, Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/moi", Jeton)).StatusCode);
    }

    [Fact]
    public async Task Deconnexion_SansSession_NEchouePas()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await NouveauClient().PostAsync("/api/auth/deconnexion", null, Jeton)).StatusCode);
    }

    [Fact]
    public async Task Cors_AutoriseLeFrontAvecCookies()
    {
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/cocktails");
        preflight.Headers.Add("Origin", "http://localhost:4200");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");

        var reponse = await NouveauClient().SendAsync(preflight, Jeton);

        Assert.Equal("http://localhost:4200", Assert.Single(reponse.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(reponse.Headers.GetValues("Access-Control-Allow-Credentials")));
    }

    [Fact]
    public async Task Cors_RefuseUneAutreOrigine()
    {
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/cocktails");
        preflight.Headers.Add("Origin", "https://site-malveillant.example");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");

        var reponse = await NouveauClient().SendAsync(preflight, Jeton);

        Assert.False(reponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static async Task<string?> Detail(HttpResponseMessage reponse) =>
        (await reponse.Content.ReadFromJsonAsync<Dictionary<string, object>>(Jeton))?["detail"]?.ToString();
}

public class LimitationDesConnexionsTests
{
    [Fact]
    public async Task AuDelaDeLaLimite_LaConnexionEstRefuseeEn429()
    {
        using var api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Comptes:0:Identifiant", "alice");
            builder.UseSetting("Comptes:0:MotDePasse", "mot-de-passe-de-test-uniquement");
            builder.UseSetting("Securite:TentativesDeConnexionParMinute", "3");
        });
        var client = api.CreateClient();

        for (int i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PostAsJsonAsync("/api/auth/connexion", new { identifiant = "alice", motDePasse = "faux-mot-de-passe" }, Jeton)).StatusCode);

        var bloquee = await client.PostAsJsonAsync("/api/auth/connexion", new { identifiant = "alice", motDePasse = "mot-de-passe-de-test-uniquement" }, Jeton);

        // Même le bon mot de passe est refusé tant que la fenêtre n'est pas écoulée.
        Assert.Equal(HttpStatusCode.TooManyRequests, bloquee.StatusCode);
    }
}

public class ConfigurationDesComptesTests
{
    [Theory]
    [InlineData("alice", "court", "au moins 12 caractères")]
    [InlineData("", "mot-de-passe-assez-long", "identifiant est obligatoire")]
    public void CompteInvalide_EmpecheLeDemarrage(string identifiant, string motDePasse, string messageAttendu)
    {
        using var api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Comptes:0:Identifiant", identifiant);
            builder.UseSetting("Comptes:0:MotDePasse", motDePasse);
        });

        var erreur = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.Contains(messageAttendu, AplatirMessages(erreur));
    }

    [Fact]
    public void IdentifiantEnDouble_EmpecheLeDemarrage()
    {
        using var api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Comptes:0:Identifiant", "alice");
            builder.UseSetting("Comptes:0:MotDePasse", "mot-de-passe-assez-long");
            builder.UseSetting("Comptes:1:Identifiant", "ALICE");
            builder.UseSetting("Comptes:1:MotDePasse", "autre-mot-de-passe-long");
        });

        var erreur = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.Contains("déclaré plusieurs fois", AplatirMessages(erreur));
    }

    private static string AplatirMessages(Exception erreur)
    {
        List<string> messages = [];
        for (Exception? courante = erreur; courante is not null; courante = courante.InnerException)
            messages.Add(courante.Message);
        return string.Join(" | ", messages);
    }
}
