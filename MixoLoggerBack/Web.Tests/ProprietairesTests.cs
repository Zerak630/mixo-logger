using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Web.Tests;

/// <summary>
/// Lot C2 : chaque utilisateur a son propre bar, et seul l'auteur d'une recette peut la
/// modifier ou la supprimer. Deux comptes de test, qui n'existent que dans les tests.
/// </summary>
public class ProprietairesTests : IClassFixture<ProprietairesTests.ApiDeTest>
{
    private const string MotDePasse = "mot-de-passe-de-test-uniquement";

    private static CancellationToken Jeton => TestContext.Current.CancellationToken;

    public class ApiDeTest : ApiAvecBaseTemporaire
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Comptes:0:Identifiant", "alice");
            builder.UseSetting("Comptes:0:NomAffiche", "Alice");
            builder.UseSetting("Comptes:0:MotDePasse", MotDePasse);
            builder.UseSetting("Comptes:1:Identifiant", "bob");
            builder.UseSetting("Comptes:1:NomAffiche", "Bob");
            builder.UseSetting("Comptes:1:MotDePasse", MotDePasse);
            builder.UseSetting("Securite:TentativesDeConnexionParMinute", "1000");
        }
    }

    private readonly ApiDeTest _api;

    public ProprietairesTests(ApiDeTest api) => _api = api;

    private async Task<HttpClient> ConnecterAsync(string identifiant)
    {
        HttpClient client = _api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var reponse = await client.PostAsJsonAsync("/api/auth/connexion", new { identifiant, motDePasse = MotDePasse }, Jeton);
        reponse.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Un nom unique : les dépôts en mémoire sont partagés par tous les tests du processus.</summary>
    private static object Recette(string prefixe = "Recette de test") => new
    {
        name = $"{prefixe} {Guid.NewGuid():N}",
        ingredients = new[] { new { name = "Rhum blanc", valeur = 5, unite = "cL" } },
        etapes = new[] { "Verser" }
    };

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage reponse) =>
        await reponse.Content.ReadFromJsonAsync<JsonElement>(Jeton);

    private static async Task<IReadOnlyList<string>> IngredientsDuBarAsync(HttpClient client) =>
        [.. (await JsonAsync(await client.GetAsync("/api/bars", Jeton)))
            .GetProperty("ingredients").EnumerateArray()
            .Select(ingredient => ingredient.GetProperty("name").GetString()!)];

    [Fact]
    public async Task ChaqueUtilisateur_ASonPropreBar()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");
        string ingredient = $"Liqueur témoin {Guid.NewGuid():N}";
        string ingredientDeBob = $"Bitter témoin {Guid.NewGuid():N}";

        // Les deux bars sont enregistrés : une écriture ne doit pas écraser le bar de l'autre.
        Assert.Equal(HttpStatusCode.OK, (await bob.PostAsJsonAsync("/api/bars/ingredients", new { name = ingredientDeBob }, Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.PostAsJsonAsync("/api/bars/ingredients", new { name = ingredient }, Jeton)).StatusCode);

        Assert.Contains(await IngredientsDuBarAsync(alice), nom => nom == ingredient);
        Assert.DoesNotContain(await IngredientsDuBarAsync(alice), nom => nom == ingredientDeBob);
        Assert.Contains(await IngredientsDuBarAsync(bob), nom => nom == ingredientDeBob);
        Assert.DoesNotContain(await IngredientsDuBarAsync(bob), nom => nom == ingredient);

        // Bob ne peut pas non plus retirer ce qui n'est que dans le bar d'Alice.
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/bars/ingredients/{Uri.EscapeDataString(ingredient)}", Jeton)).StatusCode);
        Assert.Contains(await IngredientsDuBarAsync(alice), nom => nom == ingredient);
    }

    [Fact]
    public async Task LaFaisabilite_DependDuBarDeLUtilisateur()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");
        string ingredient = $"Sirop témoin {Guid.NewGuid():N}";

        var creation = await alice.PostAsJsonAsync("/api/cocktails", new
        {
            name = $"Faisabilité {Guid.NewGuid():N}",
            ingredients = new[] { new { name = ingredient, valeur = 2, unite = "cL" } },
            etapes = new[] { "Verser" }
        }, Jeton);
        string id = (await JsonAsync(creation)).GetProperty("id").GetString()!;
        await alice.PostAsJsonAsync("/api/bars/ingredients", new { name = ingredient }, Jeton);

        static async Task<bool> RealisablePour(HttpClient client, string id) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray()
                .Single(cocktail => cocktail.GetProperty("id").GetString() == id)
                .GetProperty("realisable").GetBoolean();

        Assert.True(await RealisablePour(alice, id));
        Assert.False(await RealisablePour(bob, id));
    }

    [Fact]
    public async Task Creation_EnregistreLAuteur()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");

        var creation = await alice.PostAsJsonAsync("/api/cocktails", Recette(), Jeton);
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        JsonElement cree = await JsonAsync(creation);
        Assert.Equal("Alice", cree.GetProperty("auteur").GetString());
        Assert.True(cree.GetProperty("modifiable").GetBoolean());

        JsonElement vuParBob = await bob.GetFromJsonAsync<JsonElement>($"/api/cocktails/{cree.GetProperty("id").GetString()}", Jeton);
        Assert.Equal("Alice", vuParBob.GetProperty("auteur").GetString());
        Assert.False(vuParBob.GetProperty("modifiable").GetBoolean());
    }

    [Fact]
    public async Task SeulLAuteur_PeutModifier()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");
        string id = (await JsonAsync(await alice.PostAsJsonAsync("/api/cocktails", Recette(), Jeton))).GetProperty("id").GetString()!;

        var parBob = await bob.PutAsJsonAsync($"/api/cocktails/{id}", Recette("Détournée"), Jeton);
        Assert.Equal(HttpStatusCode.Forbidden, parBob.StatusCode);
        Assert.Equal("application/problem+json", parBob.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("Recette de test", (await alice.GetFromJsonAsync<JsonElement>($"/api/cocktails/{id}", Jeton)).GetProperty("name").GetString());

        var parAlice = await alice.PutAsJsonAsync($"/api/cocktails/{id}", Recette("Modifiée"), Jeton);
        Assert.Equal(HttpStatusCode.OK, parAlice.StatusCode);
        Assert.Equal("Alice", (await JsonAsync(parAlice)).GetProperty("auteur").GetString());
    }

    [Fact]
    public async Task SeulLAuteur_PeutSupprimer()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");
        string id = (await JsonAsync(await alice.PostAsJsonAsync("/api/cocktails", Recette(), Jeton))).GetProperty("id").GetString()!;

        Assert.Equal(HttpStatusCode.Forbidden, (await bob.DeleteAsync($"/api/cocktails/{id}", Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/api/cocktails/{id}", Jeton)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/api/cocktails/{id}", Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/cocktails/{id}", Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.DeleteAsync($"/api/cocktails/{id}", Jeton)).StatusCode);
    }

    [Fact]
    public async Task RecetteDOrigine_EstEnLectureSeule()
    {
        var alice = await ConnecterAsync("alice");
        JsonElement mojito = (await alice.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray()
            .Single(cocktail => cocktail.GetProperty("name").GetString() == "Mojito");
        string id = mojito.GetProperty("id").GetString()!;

        JsonElement detail = await alice.GetFromJsonAsync<JsonElement>($"/api/cocktails/{id}", Jeton);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("auteur").ValueKind);
        Assert.False(detail.GetProperty("modifiable").GetBoolean());

        Assert.Equal(HttpStatusCode.Forbidden, (await alice.PutAsJsonAsync($"/api/cocktails/{id}", Recette(), Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await alice.DeleteAsync($"/api/cocktails/{id}", Jeton)).StatusCode);
    }

    [Fact]
    public async Task ModificationRefusee_NAjouteRienAuReferentiel()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");
        string id = (await JsonAsync(await alice.PostAsJsonAsync("/api/cocktails", Recette(), Jeton))).GetProperty("id").GetString()!;
        string inconnu = $"Ingrédient fantôme {Guid.NewGuid():N}";

        var reponse = await bob.PutAsJsonAsync($"/api/cocktails/{id}", new
        {
            name = $"Détournée {Guid.NewGuid():N}",
            ingredients = new[] { new { name = inconnu, valeur = 1, unite = "cL" } },
            etapes = new[] { "Verser" }
        }, Jeton);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
        string referentiel = await bob.GetStringAsync("/api/ingredients", Jeton);
        Assert.DoesNotContain(inconnu, referentiel);
    }
}
