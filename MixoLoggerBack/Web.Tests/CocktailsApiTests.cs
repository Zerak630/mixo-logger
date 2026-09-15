using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using static Web.Tests.Http;

namespace Web.Tests;

/// <summary>Recettes par l'API (F1, F2, F5) : création, validation, modification, lecture, liste.</summary>
public class CocktailsApiTests(ApiAvecComptes api) : IClassFixture<ApiAvecComptes>
{
    private Task<HttpClient> AliceAsync() => api.ConnecterAsync("alice");

    private static Task<HttpResponseMessage> CreerAsync(HttpClient client, object recette) =>
        client.PostAsJsonAsync("/api/cocktails", recette, Jeton);

    private static object RecetteComplete(string nom) => new
    {
        name = nom,
        description = "  Frais et acidulé  ",
        ingredients = new object[]
        {
            new { name = "white rum", valeur = 5, unite = "cL" },
            new { name = "Citron vert", valeur = 2, unite = "cL" },
            new { name = "Menthe", valeur = 8, unite = "feuille" },
            new { name = "Bitters", valeur = 2, unite = "trait" }
        },
        etapes = new[] { "  Piler la menthe  ", "Ajouter le rhum", "Allonger" }
    };

    [Fact]
    public async Task Creation_201_AvecLocation_EtContenuNormalise()
    {
        var alice = await AliceAsync();
        string nom = Unique("Daïquiri");

        var reponse = await CreerAsync(alice, RecetteComplete($"  {nom}  "));

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        JsonElement cree = await reponse.JsonAsync();
        string id = cree.GetProperty("id").GetString()!;
        Assert.EndsWith($"/api/Cocktails/{id}", reponse.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        JsonElement relu = await alice.GetJsonAsync(reponse.Headers.Location!.ToString());
        Assert.Equal(nom, relu.GetProperty("name").GetString());
        Assert.Equal("Frais et acidulé", relu.GetProperty("description").GetString());
        // Alias résolu vers le nom canonique, ordre de saisie conservé, doses et décomptes intacts.
        Assert.Equal(["Rhum blanc", "Citron vert", "Menthe", "Bitters"], relu.GetProperty("ingredients").EnumerateArray().Select(i => i.GetProperty("name").GetString()));
        Assert.Equal(["cL", "cL", "feuille", "trait"], relu.GetProperty("ingredients").EnumerateArray().Select(i => i.GetProperty("unite").GetString()));
        Assert.Equal([1, 2, 3], relu.GetProperty("etapes").EnumerateArray().Select(e => e.GetProperty("ordre").GetInt32()));
        Assert.Equal("Piler la menthe", relu.GetProperty("etapes")[0].GetProperty("description").GetString());
    }

    [Fact]
    public async Task Creation_SansDescription_DescriptionNulle()
    {
        var alice = await AliceAsync();

        var reponse = await CreerAsync(alice, Recette(Unique("Sobre"), ("Gin", 4, "cL")));

        Assert.Equal(JsonValueKind.Null, (await reponse.JsonAsync()).GetProperty("description").ValueKind);
    }

    public static TheoryData<string, string> RecettesInvalides => new()
    {
        { """{"name":"","ingredients":[{"name":"Gin","valeur":4,"unite":"cL"}],"etapes":["Verser"]}""", "Le nom du cocktail est obligatoire." },
        { """{"name":"   ","ingredients":[{"name":"Gin","valeur":4,"unite":"cL"}],"etapes":["Verser"]}""", "Le nom du cocktail est obligatoire." },
        { """{"name":"Sans ingrédient","ingredients":[],"etapes":["Verser"]}""", "Un cocktail doit avoir au moins un ingrédient." },
        { """{"name":"Sans étape","ingredients":[{"name":"Gin","valeur":4,"unite":"cL"}],"etapes":[]}""", "Un cocktail doit avoir au moins une étape." },
        { """{"name":"Étape vide","ingredients":[{"name":"Gin","valeur":4,"unite":"cL"}],"etapes":["Verser","  "]}""", "Une étape de préparation ne peut pas être vide." },
        { """{"name":"Ingrédient sans nom","ingredients":[{"name":" ","valeur":4,"unite":"cL"}],"etapes":["Verser"]}""", "Chaque ingrédient de la recette doit avoir un nom." },
        { """{"name":"Unité inconnue","ingredients":[{"name":"Gin","valeur":4,"unite":"oz"}],"etapes":["Verser"]}""", "Unité inconnue : « oz »" },
        { """{"name":"Quantité nulle","ingredients":[{"name":"Gin","valeur":0,"unite":"cL"}],"etapes":["Verser"]}""", "La quantité doit être supérieure à zéro." },
        { """{"name":"Quantité négative","ingredients":[{"name":"Gin","valeur":-3,"unite":"cL"}],"etapes":["Verser"]}""", "La quantité doit être supérieure à zéro." },
        { """{"name":"Doublon par alias","ingredients":[{"name":"rhum","valeur":4,"unite":"cL"},{"name":"White Rum","valeur":1,"unite":"cL"}],"etapes":["Verser"]}""", "L'ingrédient « Rhum blanc » apparaît plusieurs fois" },
    };

    [Theory]
    [MemberData(nameof(RecettesInvalides))]
    public async Task Creation_Invalide_400_AvecUnMessageLisible(string corps, string message)
    {
        var alice = await AliceAsync();

        var reponse = await alice.PostAsync("/api/cocktails", new StringContent(corps, System.Text.Encoding.UTF8, "application/json"), Jeton);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        string detail = await reponse.DetailAsync();
        Assert.Contains(message, detail);
        // Le suffixe technique d'ArgumentException ne doit pas atteindre l'utilisateur.
        Assert.DoesNotContain("(Parameter", detail);
    }

    [Fact]
    public async Task Creation_Refusee_NAjouteNiRecetteNiIngredient()
    {
        var alice = await AliceAsync();
        string ingredient = Unique("Ingrédient orphelin");
        string nom = Unique("Recette refusée");
        int avant = (await alice.GetJsonAsync("/api/cocktails")).GetArrayLength();

        var reponse = await CreerAsync(alice, new { name = nom, ingredients = new[] { new { name = ingredient, valeur = 1, unite = "cL" } }, etapes = new[] { "" } });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal(avant, (await alice.GetJsonAsync("/api/cocktails")).GetArrayLength());
        Assert.DoesNotContain(ingredient, await alice.GetStringAsync("/api/ingredients", Jeton));
    }

    [Theory]
    [InlineData("Mojito")]
    [InlineData("MOJITO")]
    [InlineData(" mojito ")]
    public async Task Creation_NomDejaPris_AccentsEtCasseIgnores_409(string nom)
    {
        var alice = await AliceAsync();

        var reponse = await CreerAsync(alice, Recette(nom, ("Gin", 4, "cL")));

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
        Assert.Contains("s'appelle déjà", await reponse.DetailAsync());
    }

    [Fact]
    public async Task Creation_NomAvecAccentsDifferents_EstUnDoublon()
    {
        var alice = await AliceAsync();
        string nom = Unique("Crème brûlée");
        await alice.CreerRecetteAsync(nom, ("Gin", 4, "cL"));

        var reponse = await CreerAsync(alice, Recette(nom.Replace("è", "e").Replace("û", "u").ToUpperInvariant(), ("Gin", 4, "cL")));

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
    }

    [Fact]
    public async Task Modification_RemplaceLeContenu_ConserveIdentifiant()
    {
        var alice = await AliceAsync();
        string nom = Unique("Avant");
        string id = await alice.CreerRecetteAsync(nom, ("Gin", 4, "cL"), ("Tonic", 10, "cL"));

        var reponse = await alice.PutAsJsonAsync($"/api/cocktails/{id}", new
        {
            name = nom + " après",
            description = "Nouvelle description",
            ingredients = new[] { new { name = "Vodka", valeur = 3, unite = "cL" } },
            etapes = new[] { "Première", "Deuxième" }
        }, Jeton);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        JsonElement relu = await alice.GetJsonAsync($"/api/cocktails/{id}");
        Assert.Equal(id, relu.GetProperty("id").GetString());
        Assert.Equal(nom + " après", relu.GetProperty("name").GetString());
        Assert.Equal(["Vodka"], relu.GetProperty("ingredients").EnumerateArray().Select(i => i.GetProperty("name").GetString()));
        Assert.Equal(2, relu.GetProperty("etapes").GetArrayLength());
    }

    [Fact]
    public async Task Modification_GarderSonPropreNom_ChangeantLaCasse_EstAcceptee()
    {
        var alice = await AliceAsync();
        string nom = Unique("Casse");
        string id = await alice.CreerRecetteAsync(nom, ("Gin", 4, "cL"));

        var reponse = await alice.PutAsJsonAsync($"/api/cocktails/{id}", Recette(nom.ToUpperInvariant(), ("Gin", 5, "cL")), Jeton);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(nom.ToUpperInvariant(), (await reponse.JsonAsync()).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Modification_VersLeNomDUneAutreRecette_409_EtRienNeChange()
    {
        var alice = await AliceAsync();
        string premier = Unique("Premier nom");
        string second = Unique("Second nom");
        await alice.CreerRecetteAsync(premier, ("Gin", 4, "cL"));
        string id = await alice.CreerRecetteAsync(second, ("Gin", 4, "cL"));

        var reponse = await alice.PutAsJsonAsync($"/api/cocktails/{id}", Recette(premier, ("Vodka", 1, "cL")), Jeton);

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
        JsonElement relu = await alice.GetJsonAsync($"/api/cocktails/{id}");
        Assert.Equal(second, relu.GetProperty("name").GetString());
        Assert.Equal("Gin", relu.GetProperty("ingredients")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Modification_ContenuInvalide_400_EtRienNeChange()
    {
        var alice = await AliceAsync();
        string nom = Unique("Intacte");
        string id = await alice.CreerRecetteAsync(nom, ("Gin", 4, "cL"));

        var reponse = await alice.PutAsJsonAsync($"/api/cocktails/{id}", new { name = nom, ingredients = Array.Empty<object>(), etapes = new[] { "Verser" } }, Jeton);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal(1, (await alice.GetJsonAsync($"/api/cocktails/{id}")).GetProperty("ingredients").GetArrayLength());
    }

    [Fact]
    public async Task Modification_Et_Suppression_RecetteInconnue_404()
    {
        var alice = await AliceAsync();
        Guid inconnu = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await alice.PutAsJsonAsync($"/api/cocktails/{inconnu}", Recette(Unique("X"), ("Gin", 1, "cL")), Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.DeleteAsync($"/api/cocktails/{inconnu}", Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync($"/api/cocktails/{inconnu}", Jeton)).StatusCode);
    }

    [Fact]
    public async Task Lecture_IdentifiantMalForme_400()
    {
        var alice = await AliceAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await alice.GetAsync("/api/cocktails/pas-un-guid", Jeton)).StatusCode);
    }

    [Fact]
    public async Task Detail_NExposePasDeChampInterne()
    {
        var alice = await AliceAsync();
        string id = await alice.CreerRecetteAsync(Unique("Exposition"), ("Gin", 4, "cL"));

        string brut = await alice.GetStringAsync($"/api/cocktails/{id}", Jeton);

        Assert.DoesNotContain("authorId", brut, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("normalized", brut, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdAt", brut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unites_ListeAttendue()
    {
        var alice = await AliceAsync();

        JsonElement unites = await alice.GetJsonAsync("/api/cocktails/unites");

        Assert.Equal(["mL", "cL", "dL", "L", "piece", "feuille", "trait", "pincee"], unites.EnumerateArray().Select(u => u.GetString()));
    }

    [Fact]
    public async Task Liste_TrieRealisablesPuisMoinsDeManques()
    {
        // Carole a son propre bar : les autres tests de la classe ne le touchent pas.
        var carole = await api.ConnecterAsync("carole");
        string a = Unique("Ingrédient A");
        string b = Unique("Ingrédient B");
        string c = Unique("Ingrédient C");
        (await carole.PostAsJsonAsync("/api/bars/ingredients", new { name = a }, Jeton)).EnsureSuccessStatusCode();

        string realisable = await carole.CreerRecetteAsync(Unique("Tri 1 réalisable"), (a, 1, "cL"));
        string unManque = await carole.CreerRecetteAsync(Unique("Tri 2 un manque"), (a, 1, "cL"), (b, 1, "cL"));
        string deuxManques = await carole.CreerRecetteAsync(Unique("Tri 3 deux manques"), (b, 1, "cL"), (c, 1, "cL"));

        List<string> ordre = [.. (await carole.GetJsonAsync("/api/cocktails")).EnumerateArray().Select(x => x.GetProperty("id").GetString()!)];

        Assert.True(ordre.IndexOf(realisable) < ordre.IndexOf(unManque));
        Assert.True(ordre.IndexOf(unManque) < ordre.IndexOf(deuxManques));

        var resumeDeuxManques = (await carole.GetJsonAsync("/api/cocktails")).EnumerateArray().Single(x => x.GetProperty("id").GetString() == deuxManques);
        Assert.Equal([b, c], resumeDeuxManques.GetProperty("manques").EnumerateArray().Select(m => m.GetProperty("ingredient").GetString()));
    }

    [Fact]
    public async Task Ingredients_TriesAvecAliasNormalises()
    {
        var alice = await AliceAsync();

        List<JsonElement> ingredients = [.. (await alice.GetJsonAsync("/api/ingredients")).EnumerateArray()];

        JsonElement rhum = ingredients.Single(i => i.GetProperty("name").GetString() == "Rhum blanc");
        Assert.Equal(["rhum", "rhum agricole blanc", "white rum"], rhum.GetProperty("aliases").EnumerateArray().Select(a => a.GetString()).Order());
        Assert.Equal(ingredients.Count, ingredients.Select(i => i.GetProperty("id").GetString()).Distinct().Count());
    }
}
