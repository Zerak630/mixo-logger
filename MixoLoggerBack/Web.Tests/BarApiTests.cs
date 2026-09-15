using System.Net;
using System.Net.Http.Json;
using Xunit;
using static Web.Tests.Http;

namespace Web.Tests;

/// <summary>
/// « Mon bar » par l'API (F3, F4) : ajout, niveau, retrait, préparation de cocktails. Chaque test
/// travaille sur des ingrédients qui lui sont propres : le bar d'alice est partagé par la classe.
/// </summary>
public class BarApiTests(ApiAvecComptes api) : IClassFixture<ApiAvecComptes>
{
    private Task<HttpClient> AliceAsync() => api.ConnecterAsync("alice");

    private static Task<HttpResponseMessage> AjouterAsync(HttpClient client, object corps) =>
        client.PostAsJsonAsync("/api/bars/ingredients", corps, Jeton);

    private static Task<HttpResponseMessage> PreparerAsync(HttpClient client, params object[] commande) =>
        client.PostAsJsonAsync("/api/bars/MakeCocktails", commande, Jeton);

    [Fact]
    public async Task Ajout_SansNiveauNiVolume_PossessionSimplePleine()
    {
        var alice = await AliceAsync();
        string nom = Unique("Liqueur");

        var reponse = await AjouterAsync(alice, new { name = nom });

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var ligne = (await reponse.JsonAsync()).GetProperty("ingredients").EnumerateArray().Single(l => l.GetProperty("name").GetString() == nom);
        Assert.Equal("Pleine", ligne.GetProperty("niveau").GetString());
        Assert.False(ligne.GetProperty("suiviPrecis").GetBoolean());
        Assert.Null(ligne.VolumeEnValeur());
    }

    [Fact]
    public async Task Ajout_ParAlias_EnregistreLeNomCanonique()
    {
        var alice = await AliceAsync();

        await AjouterAsync(alice, new { name = "angostura", niveau = "Entamee" });

        var bar = await alice.BarAsync();
        Assert.True(bar.ContainsKey("Bitters"));
        Assert.False(bar.ContainsKey("angostura"));
        Assert.Equal("Entamee", bar["Bitters"].GetProperty("niveau").GetString());
    }

    [Fact]
    public async Task Ajout_AvecVolume_SuiviPrecis_EtCumulSurDeuxAjouts()
    {
        var alice = await AliceAsync();
        string nom = Unique("Rhum de test");

        await AjouterAsync(alice, new { name = nom, quantity = new { value = 70, unit = "cL" } });
        await AjouterAsync(alice, new { name = nom, quantity = new { value = 50, unit = "mL" } });

        var ligne = (await alice.BarAsync())[nom];
        Assert.True(ligne.GetProperty("suiviPrecis").GetBoolean());
        // Unités différentes : le cumul bascule en millilitres.
        Assert.Equal(750, ligne.VolumeEnValeur());
        Assert.Equal("mL", ligne.GetProperty("quantity").GetProperty("unit").GetString());
    }

    [Theory]
    [InlineData("""{"name":""}""", "nom de l'ingrédient est obligatoire")]
    [InlineData("""{"name":"   "}""", "nom de l'ingrédient est obligatoire")]
    [InlineData("""{"name":"Gin","niveau":"Moitie"}""", "Niveau inconnu : « Moitie »")]
    [InlineData("""{"name":"Gin","quantity":{"value":70,"unit":"oz"}}""", "Unité de volume inconnue : « oz »")]
    [InlineData("""{"name":"Gin","quantity":{"value":-1,"unit":"cL"}}""", "ne peut pas être négatif")]
    public async Task Ajout_Invalide_400_SansRienEnregistrer(string corps, string extraitDuDetail)
    {
        var alice = await AliceAsync();
        int avant = (await alice.BarAsync()).Count;

        var reponse = await alice.PostAsync("/api/bars/ingredients", new StringContent(corps, System.Text.Encoding.UTF8, "application/json"), Jeton);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains(extraitDuDetail, await reponse.DetailAsync());
        Assert.Equal(avant, (await alice.BarAsync()).Count);
    }

    [Theory]
    [InlineData("niveau", "Moitie")]
    [InlineData("unite", "oz")]
    public async Task Ajout_Refuse_NAjouteRienAuReferentiel(string champ, string valeurInvalide)
    {
        var alice = await AliceAsync();
        string nom = Unique("Ingrédient refusé");
        object corps = champ == "niveau"
            ? new { name = nom, niveau = valeurInvalide }
            : new { name = nom, quantity = new { value = 70, unit = valeurInvalide } };

        Assert.Equal(HttpStatusCode.BadRequest, (await AjouterAsync(alice, corps)).StatusCode);

        // Sinon l'ingrédient d'une saisie refusée apparaîtrait dans l'autocomplétion de tout le monde.
        Assert.DoesNotContain(nom, await alice.GetStringAsync("/api/ingredients", Jeton));
    }

    [Fact]
    public async Task Niveau_ModifieLaLigne_EtConserveLeVolume()
    {
        var alice = await AliceAsync();
        string nom = Unique("Cognac");
        await AjouterAsync(alice, new { name = nom, quantity = new { value = 35, unit = "cL" } });

        var reponse = await alice.PatchAsJsonAsync($"/api/bars/ingredients/{Uri.EscapeDataString(nom)}", new { niveau = "PresqueFinie" }, Jeton);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var ligne = (await alice.BarAsync())[nom];
        Assert.Equal("PresqueFinie", ligne.GetProperty("niveau").GetString());
        Assert.Equal(35, ligne.VolumeEnValeur());
    }

    [Fact]
    public async Task Niveau_ParAlias_DesigneLaLigneCanonique()
    {
        var alice = await AliceAsync();
        await AjouterAsync(alice, new { name = "Rhum ambré" });

        var reponse = await alice.PatchAsJsonAsync("/api/bars/ingredients/dark%20rum", new { niveau = "Entamee" }, Jeton);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Entamee", (await alice.BarAsync())["Rhum ambré"].GetProperty("niveau").GetString());
    }

    [Fact]
    public async Task Niveau_IngredientInconnu_OuAbsentDuBar_404()
    {
        var alice = await AliceAsync();

        var inconnu = await alice.PatchAsJsonAsync($"/api/bars/ingredients/{Uri.EscapeDataString(Unique("Inconnu"))}", new { niveau = "Pleine" }, Jeton);
        // « Bourbon » existe dans le référentiel mais aucun test ne le met dans le bar.
        var absent = await alice.PatchAsJsonAsync("/api/bars/ingredients/Bourbon", new { niveau = "Pleine" }, Jeton);

        Assert.Equal(HttpStatusCode.NotFound, inconnu.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
        Assert.Contains("n'est pas dans le bar", await absent.DetailAsync());
    }

    [Fact]
    public async Task Niveau_Invalide_400()
    {
        var alice = await AliceAsync();
        string nom = Unique("Vermouth");
        await AjouterAsync(alice, new { name = nom });

        var reponse = await alice.PatchAsJsonAsync($"/api/bars/ingredients/{Uri.EscapeDataString(nom)}", new { niveau = "Vide" }, Jeton);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal("Pleine", (await alice.BarAsync())[nom].GetProperty("niveau").GetString());
    }

    [Fact]
    public async Task Retrait_PuisSecondRetrait_404()
    {
        var alice = await AliceAsync();
        string nom = Unique("Absinthe");
        await AjouterAsync(alice, new { name = nom });

        var premier = await alice.DeleteAsync($"/api/bars/ingredients/{Uri.EscapeDataString(nom)}", Jeton);
        var second = await alice.DeleteAsync($"/api/bars/ingredients/{Uri.EscapeDataString(nom)}", Jeton);

        Assert.Equal(HttpStatusCode.OK, premier.StatusCode);
        Assert.False((await alice.BarAsync()).ContainsKey(nom));
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task Preparation_DecompteLesVolumesSuivis_PasLesPossessionsNiLesDecomptes()
    {
        var alice = await AliceAsync();
        string alcool = Unique("Alcool suivi");
        string sirop = Unique("Sirop simple");
        string feuille = Unique("Feuille");
        string id = await alice.CreerRecetteAsync(Unique("Recette préparée"), (alcool, 4, "cL"), (sirop, 1, "cL"), (feuille, 6, "feuille"));

        await AjouterAsync(alice, new { name = alcool, quantity = new { value = 20, unit = "cL" } });
        await AjouterAsync(alice, new { name = sirop });
        await AjouterAsync(alice, new { name = feuille });

        var reponse = await PreparerAsync(alice, new { cocktailId = id, quantity = 3 });

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var bar = await alice.BarAsync();
        Assert.Equal(8, bar[alcool].VolumeEnValeur());      // 20 cL - 3 × 4 cL
        Assert.Null(bar[sirop].VolumeEnValeur());
        Assert.True(bar.ContainsKey(feuille));
    }

    [Fact]
    public async Task Preparation_Insuffisante_409_EtRienNestDecompte()
    {
        var alice = await AliceAsync();
        string alcool = Unique("Alcool rare");
        string id = await alice.CreerRecetteAsync(Unique("Trop gourmande"), (alcool, 5, "cL"));
        await AjouterAsync(alice, new { name = alcool, quantity = new { value = 12, unit = "cL" } });

        var reponse = await PreparerAsync(alice, new { cocktailId = id, quantity = 3 });

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
        Assert.Contains("quantité insuffisante", await reponse.DetailAsync());
        Assert.Equal(12, (await alice.BarAsync())[alcool].VolumeEnValeur());
    }

    [Fact]
    public async Task Preparation_CommandeDePlusieursCocktails_ToutOuRien()
    {
        var alice = await AliceAsync();
        string partage = Unique("Base partagée");
        string premier = await alice.CreerRecetteAsync(Unique("Premier"), (partage, 5, "cL"));
        string second = await alice.CreerRecetteAsync(Unique("Second"), (partage, 5, "cL"));
        await AjouterAsync(alice, new { name = partage, quantity = new { value = 8, unit = "cL" } });

        // Chaque recette seule passerait ; ensemble, elles demandent 10 cL pour 8 disponibles.
        var reponse = await PreparerAsync(alice, new { cocktailId = premier, quantity = 1 }, new { cocktailId = second, quantity = 1 });

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
        Assert.Equal(8, (await alice.BarAsync())[partage].VolumeEnValeur());
    }

    [Fact]
    public async Task Preparation_IngredientAbsent_409_NommeLIngredient()
    {
        var alice = await AliceAsync();
        string manquant = Unique("Introuvable");
        string id = await alice.CreerRecetteAsync(Unique("Impossible"), (manquant, 2, "cL"));

        var reponse = await PreparerAsync(alice, new { cocktailId = id, quantity = 1 });

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
        Assert.Contains($"« {manquant} » absent", await reponse.DetailAsync());
    }

    [Fact]
    public async Task Preparation_CocktailInconnu_404()
    {
        var alice = await AliceAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await PreparerAsync(alice, new { cocktailId = Guid.NewGuid(), quantity = 1 })).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Preparation_QuantiteInvalide_400(int quantite)
    {
        var alice = await AliceAsync();
        string ingredient = Unique("Ingrédient");
        string id = await alice.CreerRecetteAsync(Unique("Quantité"), (ingredient, 1, "cL"));
        await AjouterAsync(alice, new { name = ingredient });

        Assert.Equal(HttpStatusCode.BadRequest, (await PreparerAsync(alice, new { cocktailId = id, quantity = quantite })).StatusCode);
    }

    [Fact]
    public async Task Faisabilite_DansLaListe_SuitLeStock()
    {
        var alice = await AliceAsync();
        string alcool = Unique("Alcool compté");
        string id = await alice.CreerRecetteAsync(Unique("Faisabilité"), (alcool, 5, "cL"));

        async Task<(bool Realisable, string? Raison)> EtatAsync()
        {
            var cocktail = (await alice.GetJsonAsync("/api/cocktails")).EnumerateArray().Single(c => c.GetProperty("id").GetString() == id);
            var manques = cocktail.GetProperty("manques").EnumerateArray().ToList();
            return (cocktail.GetProperty("realisable").GetBoolean(), manques.SingleOrDefault().ValueKind == System.Text.Json.JsonValueKind.Undefined ? null : manques.Single().GetProperty("raison").GetString());
        }

        Assert.Equal((false, "Absent"), await EtatAsync());

        await AjouterAsync(alice, new { name = alcool, quantity = new { value = 3, unit = "cL" } });
        Assert.Equal((false, "Insuffisant"), await EtatAsync());

        await AjouterAsync(alice, new { name = alcool, quantity = new { value = 2, unit = "cL" } });
        Assert.Equal((true, null), await EtatAsync());

        await PreparerAsync(alice, new { cocktailId = id, quantity = 1 });
        Assert.Equal((false, "Insuffisant"), await EtatAsync());
    }
}
