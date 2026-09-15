using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Web.Tests;

/// <summary>API sur base temporaire avec trois comptes de test (alice, bob, carole). Ils n'existent que dans les tests.</summary>
public class ApiAvecComptes : ApiAvecBaseTemporaire
{
    public const string MotDePasse = "mot-de-passe-de-test-uniquement";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        string[] comptes = ["alice", "bob", "carole"];
        for (int i = 0; i < comptes.Length; i++)
        {
            builder.UseSetting($"Comptes:{i}:Identifiant", comptes[i]);
            builder.UseSetting($"Comptes:{i}:MotDePasse", MotDePasse);
        }
        builder.UseSetting("Securite:TentativesDeConnexionParMinute", "1000");
    }

    public async Task<HttpClient> ConnecterAsync(string identifiant)
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/connexion", new { identifiant, motDePasse = MotDePasse }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        return client;
    }
}

/// <summary>Raccourcis communs aux tests d'API.</summary>
internal static class Http
{
    public static CancellationToken Jeton => TestContext.Current.CancellationToken;

    public static string Unique(string prefixe) => $"{prefixe} {Guid.NewGuid():N}";

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage reponse) =>
        await reponse.Content.ReadFromJsonAsync<JsonElement>(Jeton);

    public static Task<JsonElement> GetJsonAsync(this HttpClient client, string url) =>
        client.GetFromJsonAsync<JsonElement>(url, Jeton);

    /// <summary>Le détail d'un <c>ProblemDetails</c>, après vérification du type de contenu.</summary>
    public static async Task<string> DetailAsync(this HttpResponseMessage reponse)
    {
        Assert.Equal("application/problem+json", reponse.Content.Headers.ContentType?.MediaType);
        return (await reponse.JsonAsync()).GetProperty("detail").GetString()!;
    }

    public static object Recette(string nom, params (string Nom, double Valeur, string Unite)[] ingredients) => new
    {
        name = nom,
        ingredients = ingredients.Select(i => new { name = i.Nom, valeur = i.Valeur, unite = i.Unite }).ToArray(),
        etapes = new[] { "Verser" }
    };

    /// <summary>Crée une recette et renvoie son identifiant.</summary>
    public static async Task<string> CreerRecetteAsync(this HttpClient client, string nom, params (string Nom, double Valeur, string Unite)[] ingredients)
    {
        var reponse = await client.PostAsJsonAsync("/api/cocktails", Recette(nom, ingredients), Jeton);
        Assert.Equal(System.Net.HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.JsonAsync()).GetProperty("id").GetString()!;
    }

    /// <summary>Lignes du bar, par nom.</summary>
    public static async Task<Dictionary<string, JsonElement>> BarAsync(this HttpClient client) =>
        (await client.GetJsonAsync("/api/bars")).GetProperty("ingredients").EnumerateArray()
            .ToDictionary(ligne => ligne.GetProperty("name").GetString()!);

    public static double? VolumeEnValeur(this JsonElement ligne) =>
        ligne.GetProperty("quantity").ValueKind == JsonValueKind.Null ? null : ligne.GetProperty("quantity").GetProperty("value").GetDouble();
}
