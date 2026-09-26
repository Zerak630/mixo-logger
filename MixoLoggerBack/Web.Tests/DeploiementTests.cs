using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Web.Tests.Http;

namespace Web.Tests;

/// <summary>
/// L'API derrière un reverse proxy (docs/DEPLOIEMENT.md) : en-têtes transférés crus seulement depuis
/// le proxy, clés des cookies conservées sur disque, pas de Swagger en production.
/// </summary>
public class DeploiementTests
{
    private const string AdresseDuProxy = "172.30.0.2";

    /// <summary>Le serveur de test n'a pas d'adresse distante : on lui donne celle d'où la requête est censée venir.</summary>
    private sealed class AdresseDistante(string adresse) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> suite) => app =>
        {
            app.Use((contexte, suivant) =>
            {
                contexte.Connection.RemoteIpAddress = IPAddress.Parse(adresse);
                return suivant(contexte);
            });
            suite(app);
        };
    }

    /// <summary>Limite par IP à 3 par minute ; la requête arrive de <paramref name="depuis"/>.</summary>
    private static WebApplicationFactory<Program> Api(ApiAvecBaseTemporaire baseDeDonnees, string depuis, string? reseauDeConfiance) =>
        baseDeDonnees.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Securite:TentativesDeConnexionParMinute", "3");
            if (reseauDeConfiance is not null)
                builder.UseSetting("ReverseProxy:ReseauxDeConfiance:0", reseauDeConfiance);
            builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(new AdresseDistante(depuis)));
        });

    /// <summary>Une tentative de connexion vue comme venant de <paramref name="client"/> (X-Forwarded-For).</summary>
    private static async Task<HttpStatusCode> ConnexionDepuisAsync(HttpClient http, string client)
    {
        using HttpRequestMessage requete = new(HttpMethod.Post, "/api/auth/connexion")
        {
            // Identifiant unique à chaque essai : seule la limite par IP doit jouer.
            Content = JsonContent.Create(new { identifiant = Unique("inconnu"), motDePasse = "faux-mot-de-passe" })
        };
        requete.Headers.Add("X-Forwarded-For", client);
        return (await http.SendAsync(requete, Jeton)).StatusCode;
    }

    [Fact]
    public async Task DepuisLeProxy_LaLimiteParIpPorteSurLeVraiClient()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Api(baseDeDonnees, depuis: AdresseDuProxy, reseauDeConfiance: "172.30.0.0/24");
        var http = api.CreateClient();

        for (int i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, await ConnexionDepuisAsync(http, "203.0.113.1"));

        Assert.Equal(HttpStatusCode.TooManyRequests, await ConnexionDepuisAsync(http, "203.0.113.1"));
        // Un autre client, derrière le même proxy, n'est pas bloqué.
        Assert.Equal(HttpStatusCode.Unauthorized, await ConnexionDepuisAsync(http, "203.0.113.2"));
    }

    [Theory]
    [InlineData(null)]                 // Aucun proxy déclaré.
    [InlineData("10.0.0.0/8")]        // Un proxy déclaré, mais la requête n'en vient pas.
    public async Task HorsDuProxy_XForwardedForEstIgnore(string? reseauDeConfiance)
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Api(baseDeDonnees, depuis: "198.51.100.7", reseauDeConfiance);
        var http = api.CreateClient();

        // Changer d'X-Forwarded-For à chaque essai ne contourne pas la limite : l'en-tête n'est pas cru.
        for (int i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, await ConnexionDepuisAsync(http, $"203.0.113.{i + 10}"));

        Assert.Equal(HttpStatusCode.TooManyRequests, await ConnexionDepuisAsync(http, "203.0.113.99"));
    }

    [Fact]
    public void ReseauInvalide_EmpecheLeDemarrage()
    {
        using var baseDeDonnees = new ApiAvecBaseTemporaire();
        using var api = baseDeDonnees.WithWebHostBuilder(builder => builder.UseSetting("ReverseProxy:ReseauxDeConfiance:0", "pas-un-reseau"));

        var erreur = Assert.ThrowsAny<Exception>(() =>
        {
            using var client = api.CreateClient();
            client.GetAsync("/ping", Jeton).GetAwaiter().GetResult();
        });

        Assert.Contains("n'est pas un réseau CIDR valide", Messages(erreur));
    }

    [Fact]
    public async Task ClesDesCookies_ConserveesDansLeDossierConfigure()
    {
        string dossier = Path.Combine(Path.GetTempPath(), $"mixologger-cles-{Guid.NewGuid():N}");

        try
        {
            await using var baseDeDonnees = new ApiAvecBaseTemporaire();
            await using var api = baseDeDonnees.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("DataProtection:Dossier", dossier);
                builder.UseSetting("Comptes:0:Identifiant", "alice");
                builder.UseSetting("Comptes:0:MotDePasse", ApiAvecComptes.MotDePasse);
            });

            var reponse = await api.CreateClient().PostAsJsonAsync("/api/auth/connexion", new { identifiant = "alice", motDePasse = ApiAvecComptes.MotDePasse }, Jeton);

            reponse.EnsureSuccessStatusCode();
            Assert.NotEmpty(Directory.GetFiles(dossier, "key-*.xml"));
        }
        finally
        {
            try { Directory.Delete(dossier, recursive: true); } catch (IOException) { }
        }
    }

    [Theory]
    // En production, la route n'existe pas : comme toute adresse inconnue, elle tombe sur la politique
    // « tout protégé par défaut » et répond 401 sans rien servir.
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.Unauthorized)]
    public async Task Swagger_SeulementEnDeveloppement(string environnement, HttpStatusCode attendu)
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = baseDeDonnees.WithWebHostBuilder(builder => builder.UseEnvironment(environnement));

        var reponse = await api.CreateClient().GetAsync("/swagger/v1/swagger.json", Jeton);

        Assert.Equal(attendu, reponse.StatusCode);
        Assert.Equal(attendu == HttpStatusCode.OK, (await reponse.Content.ReadAsStringAsync(Jeton)).Contains("MixoLogger API"));
    }

    private static string Messages(Exception erreur)
    {
        List<string> messages = [];
        for (Exception? courante = erreur; courante is not null; courante = courante.InnerException)
            messages.Add(courante.Message);
        return string.Join(" | ", messages);
    }
}
