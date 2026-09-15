using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Web.Tests;

/// <summary>
/// F10 : les données survivent à un redémarrage de l'API, le jeu initial n'est inséré qu'une
/// fois, et les contraintes de la base tiennent sous des écritures concurrentes.
/// </summary>
public class PersistanceTests
{
    private const string MotDePasse = "mot-de-passe-de-test-uniquement";

    private static CancellationToken Jeton => TestContext.Current.CancellationToken;

    /// <summary>Une instance de l'API sur <paramref name="fichier"/>, avec les comptes alice et bob.</summary>
    private static WebApplicationFactory<Program> Demarrer(ApiAvecBaseTemporaire baseDeDonnees) =>
        baseDeDonnees.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Comptes:0:Identifiant", "alice");
            builder.UseSetting("Comptes:0:MotDePasse", MotDePasse);
            builder.UseSetting("Comptes:1:Identifiant", "bob");
            builder.UseSetting("Comptes:1:MotDePasse", MotDePasse);
            builder.UseSetting("Securite:TentativesDeConnexionParMinute", "1000");
        });

    private static async Task<HttpClient> ConnecterAsync(WebApplicationFactory<Program> api, string identifiant)
    {
        HttpClient client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/connexion", new { identifiant, motDePasse = MotDePasse }, Jeton)).EnsureSuccessStatusCode();
        return client;
    }

    private static object Recette(string nom, string ingredient = "Gin") => new
    {
        name = nom,
        description = "Pour vérifier la persistance",
        ingredients = new object[]
        {
            new { name = ingredient, valeur = 4.5, unite = "cL" },
            new { name = "Menthe", valeur = 6, unite = "feuille" }
        },
        etapes = new[] { "Verser", "Mélanger" }
    };

    [Fact]
    public async Task LesDonnees_SurviventAuRedemarrage()
    {
        string fichier = ApiAvecBaseTemporaire.NouveauFichier();

        try
        {
            string id;

            // Première vie de l'API : une recette, un bar, une note.
            await using (var baseDeDonnees = new ApiAvecBaseTemporaire(fichier))
            await using (var api = Demarrer(baseDeDonnees))
            {
                var alice = await ConnecterAsync(api, "alice");

                var creation = await alice.PostAsJsonAsync("/api/cocktails", Recette("Gin Smash maison", "Liqueur de sureau"), Jeton);
                Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
                id = (await creation.Content.ReadFromJsonAsync<JsonElement>(Jeton)).GetProperty("id").GetString()!;

                (await alice.PostAsJsonAsync("/api/bars/ingredients", new { name = "white rum", quantity = new { value = 70, unit = "cL" } }, Jeton)).EnsureSuccessStatusCode();
                (await alice.PostAsJsonAsync("/api/bars/ingredients", new { name = "Gin", niveau = "PresqueFinie" }, Jeton)).EnsureSuccessStatusCode();
                (await alice.PutAsJsonAsync($"/api/cocktails/{id}/note", new { valeur = 4 }, Jeton)).EnsureSuccessStatusCode();
            }

            // Seconde vie, sur le même fichier.
            await using (var baseDeDonnees = new ApiAvecBaseTemporaire(fichier))
            await using (var api = Demarrer(baseDeDonnees))
            {
                var alice = await ConnecterAsync(api, "alice");

                JsonElement recette = await alice.GetFromJsonAsync<JsonElement>($"/api/cocktails/{id}", Jeton);
                Assert.Equal("Gin Smash maison", recette.GetProperty("name").GetString());
                Assert.Equal("Pour vérifier la persistance", recette.GetProperty("description").GetString());
                Assert.True(recette.GetProperty("modifiable").GetBoolean());
                Assert.Equal(4, recette.GetProperty("notes").GetProperty("maNote").GetInt32());

                // Ordre des ingrédients, doses (décimale et décompte) et étapes conservés.
                var ingredients = recette.GetProperty("ingredients").EnumerateArray().ToList();
                Assert.Equal(["Liqueur de sureau", "Menthe"], ingredients.Select(i => i.GetProperty("name").GetString()));
                Assert.Equal(4.5, ingredients[0].GetProperty("valeur").GetDouble());
                Assert.Equal("feuille", ingredients[1].GetProperty("unite").GetString());
                Assert.Equal(["Verser", "Mélanger"], recette.GetProperty("etapes").EnumerateArray().Select(e => e.GetProperty("description").GetString()));

                // L'ingrédient créé par la recette est resté dans le référentiel.
                Assert.Contains("Liqueur de sureau", await alice.GetStringAsync("/api/ingredients", Jeton));

                JsonElement bar = await alice.GetFromJsonAsync<JsonElement>("/api/bars", Jeton);
                var lignes = bar.GetProperty("ingredients").EnumerateArray().ToDictionary(l => l.GetProperty("name").GetString()!);
                Assert.Equal("PresqueFinie", lignes["Gin"].GetProperty("niveau").GetString());
                Assert.True(lignes["Rhum blanc"].GetProperty("suiviPrecis").GetBoolean());
                Assert.Equal(70, lignes["Rhum blanc"].GetProperty("quantity").GetProperty("value").GetDouble());

                // Le bar relu a gardé sa version : une modification après redémarrage passe.
                Assert.Equal(HttpStatusCode.OK, (await alice.DeleteAsync("/api/bars/ingredients/Gin", Jeton)).StatusCode);
            }
        }
        finally
        {
            ApiAvecBaseTemporaire.SupprimerBase(fichier);
        }
    }

    [Fact]
    public async Task LeJeuInitial_NEstInsereQuUneFois()
    {
        string fichier = ApiAvecBaseTemporaire.NouveauFichier();

        try
        {
            async Task<(int Cocktails, int Ingredients, int Mojitos)> CompterAsync()
            {
                await using var baseDeDonnees = new ApiAvecBaseTemporaire(fichier);
                await using var api = Demarrer(baseDeDonnees);
                var bob = await ConnecterAsync(api, "bob");

                var cocktails = (await bob.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray().ToList();
                int ingredients = (await bob.GetFromJsonAsync<JsonElement>("/api/ingredients", Jeton)).GetArrayLength();

                return (cocktails.Count, ingredients, cocktails.Count(c => c.GetProperty("name").GetString() == "Mojito"));
            }

            var premier = await CompterAsync();
            var second = await CompterAsync();

            Assert.Equal(5, premier.Cocktails);
            Assert.Equal(1, premier.Mojitos);
            Assert.Equal(premier, second);
        }
        finally
        {
            ApiAvecBaseTemporaire.SupprimerBase(fichier);
        }
    }

    [Fact]
    public async Task JeuInitial_AliasResolus()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var bob = await ConnecterAsync(api, "bob");

        string mojito = (await bob.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray()
            .Single(c => c.GetProperty("name").GetString() == "Mojito").GetProperty("id").GetString()!;

        // Le seed écrit « Rhum » et « Sucre » : ils doivent désigner les ingrédients canoniques.
        var noms = (await bob.GetFromJsonAsync<JsonElement>($"/api/cocktails/{mojito}", Jeton))
            .GetProperty("ingredients").EnumerateArray().Select(i => i.GetProperty("name").GetString()).ToList();

        Assert.Equal(["Rhum blanc", "Menthe", "Citron vert", "Sirop de sucre", "Eau gazeuse"], noms);
    }

    [Fact]
    public async Task EcrituresConcurrentesSurLeBar_AucunePerdueEnSilence()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var alice = await ConnecterAsync(api, "alice");

        // La première écriture crée le bar ; les suivantes testent la version.
        (await alice.PostAsJsonAsync("/api/bars/ingredients", new { name = "Eau" }, Jeton)).EnsureSuccessStatusCode();

        string[] noms = [.. Enumerable.Range(0, 30).Select(i => $"Ingrédient concurrent {i}")];
        HttpResponseMessage[] reponses = await Task.WhenAll(noms.Select(nom => alice.PostAsJsonAsync("/api/bars/ingredients", new { name = nom }, Jeton)));

        Assert.All(reponses, reponse => Assert.Contains(reponse.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));

        var enregistres = (await alice.GetFromJsonAsync<JsonElement>("/api/bars", Jeton))
            .GetProperty("ingredients").EnumerateArray().Select(l => l.GetProperty("name").GetString()).ToHashSet();

        // Chaque 200 correspond à une ligne réellement enregistrée, chaque 409 à une ligne absente.
        for (int i = 0; i < noms.Length; i++)
            Assert.Equal(reponses[i].StatusCode == HttpStatusCode.OK, enregistres.Contains(noms[i]));
    }

    [Fact]
    public async Task CreationsSimultaneesDuMemeNom_UneSeuleReussit()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var alice = await ConnecterAsync(api, "alice");
        var bob = await ConnecterAsync(api, "bob");

        HttpResponseMessage[] reponses = await Task.WhenAll(Enumerable.Range(0, 12).Select(i =>
            (i % 2 == 0 ? alice : bob).PostAsJsonAsync("/api/cocktails", Recette(i % 3 == 0 ? "Negroni blanc" : "NEGRONI  Blanc"), Jeton)));

        Assert.Single(reponses, reponse => reponse.StatusCode == HttpStatusCode.Created);
        Assert.All(reponses.Where(reponse => reponse.StatusCode != HttpStatusCode.Created),
            reponse => Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode));

        int homonymes = (await alice.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray()
            .Count(c => c.GetProperty("name").GetString()!.Equals("Negroni blanc", StringComparison.OrdinalIgnoreCase)
                || c.GetProperty("name").GetString() == "NEGRONI  Blanc");
        Assert.Equal(1, homonymes);
    }

    [Fact]
    public async Task IngredientInconnuSaisiEnParallele_CreeUneSeuleFois()
    {
        await using var baseDeDonnees = new ApiAvecBaseTemporaire();
        await using var api = Demarrer(baseDeDonnees);
        var alice = await ConnecterAsync(api, "alice");
        var bob = await ConnecterAsync(api, "bob");

        // Deux utilisateurs, donc deux bars : aucun conflit de version, seul le référentiel est partagé.
        HttpResponseMessage[] reponses = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(i => (i == 0 ? alice : bob).PostAsJsonAsync("/api/bars/ingredients", new { name = "Liqueur de violette" }, Jeton)));

        Assert.All(reponses, reponse => Assert.Equal(HttpStatusCode.OK, reponse.StatusCode));

        int occurrences = (await alice.GetFromJsonAsync<JsonElement>("/api/ingredients", Jeton)).EnumerateArray()
            .Count(i => i.GetProperty("name").GetString() == "Liqueur de violette");
        Assert.Equal(1, occurrences);
    }
}
