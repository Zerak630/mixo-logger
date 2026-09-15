using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Web.Tests;

/// <summary>
/// F7 : chaque utilisateur note un cocktail de 1 à 5 ; la moyenne et le nombre de notes sont
/// communs, la note personnelle ne l'est pas.
/// </summary>
public class NotesTests : IClassFixture<NotesTests.ApiDeTest>
{
    private const string MotDePasse = "mot-de-passe-de-test-uniquement";

    private static CancellationToken Jeton => TestContext.Current.CancellationToken;

    public class ApiDeTest : ApiAvecBaseTemporaire
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Comptes:0:Identifiant", "alice");
            builder.UseSetting("Comptes:0:MotDePasse", MotDePasse);
            builder.UseSetting("Comptes:1:Identifiant", "bob");
            builder.UseSetting("Comptes:1:MotDePasse", MotDePasse);
            builder.UseSetting("Securite:TentativesDeConnexionParMinute", "1000");
        }
    }

    private readonly ApiDeTest _api;

    public NotesTests(ApiDeTest api) => _api = api;

    private async Task<HttpClient> ConnecterAsync(string identifiant)
    {
        HttpClient client = _api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        (await client.PostAsJsonAsync("/api/auth/connexion", new { identifiant, motDePasse = MotDePasse }, Jeton)).EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Une recette neuve pour chaque test : les dépôts en mémoire sont partagés par tout le processus.</summary>
    private static async Task<string> CreerRecetteAsync(HttpClient client)
    {
        var reponse = await client.PostAsJsonAsync("/api/cocktails", new
        {
            name = $"À noter {Guid.NewGuid():N}",
            ingredients = new[] { new { name = "Gin", valeur = 4, unite = "cL" } },
            etapes = new[] { "Verser" }
        }, Jeton);
        reponse.EnsureSuccessStatusCode();
        return (await reponse.Content.ReadFromJsonAsync<JsonElement>(Jeton)).GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> NoterAsync(HttpClient client, string id, int valeur)
    {
        var reponse = await client.PutAsJsonAsync($"/api/cocktails/{id}/note", new { valeur }, Jeton);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        return await reponse.Content.ReadFromJsonAsync<JsonElement>(Jeton);
    }

    private static async Task<JsonElement> NotesDansLaListeAsync(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray()
            .Single(cocktail => cocktail.GetProperty("id").GetString() == id)
            .GetProperty("notes");

    private static double? Moyenne(JsonElement notes) =>
        notes.GetProperty("moyenne").ValueKind == JsonValueKind.Null ? null : notes.GetProperty("moyenne").GetDouble();

    private static int? MaNote(JsonElement notes) =>
        notes.GetProperty("maNote").ValueKind == JsonValueKind.Null ? null : notes.GetProperty("maNote").GetInt32();

    [Fact]
    public async Task RecetteNeuve_NAPasDeNote()
    {
        var alice = await ConnecterAsync("alice");
        string id = await CreerRecetteAsync(alice);

        JsonElement notes = (await alice.GetFromJsonAsync<JsonElement>($"/api/cocktails/{id}", Jeton)).GetProperty("notes");

        Assert.Null(Moyenne(notes));
        Assert.Equal(0, notes.GetProperty("nombre").GetInt32());
        Assert.Null(MaNote(notes));
    }

    [Fact]
    public async Task Moyenne_Commune_NotePersonnelle_Distincte()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");
        string id = await CreerRecetteAsync(alice);

        await NoterAsync(alice, id, 5);
        JsonElement apresBob = await NoterAsync(bob, id, 2);

        Assert.Equal(3.5, Moyenne(apresBob));
        Assert.Equal(2, apresBob.GetProperty("nombre").GetInt32());
        Assert.Equal(2, MaNote(apresBob));

        JsonElement vuParAlice = await NotesDansLaListeAsync(alice, id);
        Assert.Equal(3.5, Moyenne(vuParAlice));
        Assert.Equal(5, MaNote(vuParAlice));

        JsonElement detailPourBob = (await bob.GetFromJsonAsync<JsonElement>($"/api/cocktails/{id}", Jeton)).GetProperty("notes");
        Assert.Equal(2, MaNote(detailPourBob));
    }

    [Fact]
    public async Task NoterANouveau_RemplaceLaNote()
    {
        var alice = await ConnecterAsync("alice");
        string id = await CreerRecetteAsync(alice);

        await NoterAsync(alice, id, 1);
        JsonElement notes = await NoterAsync(alice, id, 4);

        Assert.Equal(1, notes.GetProperty("nombre").GetInt32());
        Assert.Equal(4.0, Moyenne(notes));
        Assert.Equal(4, MaNote(notes));
    }

    [Fact]
    public async Task RetirerSaNote_NeTouchePasCelleDesAutres()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");
        string id = await CreerRecetteAsync(alice);
        await NoterAsync(alice, id, 5);
        await NoterAsync(bob, id, 3);

        var reponse = await bob.DeleteAsync($"/api/cocktails/{id}/note", Jeton);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        JsonElement notes = await reponse.Content.ReadFromJsonAsync<JsonElement>(Jeton);

        Assert.Equal(1, notes.GetProperty("nombre").GetInt32());
        Assert.Equal(5.0, Moyenne(notes));
        Assert.Null(MaNote(notes));

        // Idempotent.
        Assert.Equal(HttpStatusCode.OK, (await bob.DeleteAsync($"/api/cocktails/{id}/note", Jeton)).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task NoteHorsBornes_400(int valeur)
    {
        var alice = await ConnecterAsync("alice");
        string id = await CreerRecetteAsync(alice);

        var reponse = await alice.PutAsJsonAsync($"/api/cocktails/{id}/note", new { valeur }, Jeton);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal(0, (await NotesDansLaListeAsync(alice, id)).GetProperty("nombre").GetInt32());
    }

    [Fact]
    public async Task CocktailInconnu_404()
    {
        var alice = await ConnecterAsync("alice");

        Assert.Equal(HttpStatusCode.NotFound, (await alice.PutAsJsonAsync($"/api/cocktails/{Guid.NewGuid()}/note", new { valeur = 4 }, Jeton)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.DeleteAsync($"/api/cocktails/{Guid.NewGuid()}/note", Jeton)).StatusCode);
    }

    [Fact]
    public async Task RecetteDOrigine_PeutEtreNotee()
    {
        var bob = await ConnecterAsync("bob");
        string id = (await bob.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray()
            .Single(cocktail => cocktail.GetProperty("name").GetString() == "Cosmopolitan")
            .GetProperty("id").GetString()!;

        Assert.Equal(4, MaNote(await NoterAsync(bob, id, 4)));
        await bob.DeleteAsync($"/api/cocktails/{id}/note", Jeton);
    }

    [Fact]
    public async Task SupprimerLaRecette_SupprimeSesNotes()
    {
        var alice = await ConnecterAsync("alice");
        string id = await CreerRecetteAsync(alice);
        await NoterAsync(alice, id, 5);

        using IServiceScope portee = _api.Services.CreateScope();
        INoteRepository depot = portee.ServiceProvider.GetRequiredService<INoteRepository>();
        Assert.Single(await depot.GetByCocktailAsync(Guid.Parse(id)));

        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/api/cocktails/{id}", Jeton)).StatusCode);

        // Aucune note orpheline ne doit rester : l'API ne permet plus de les voir, on vérifie le dépôt.
        Assert.Empty(await depot.GetByCocktailAsync(Guid.Parse(id)));
    }

    [Fact]
    public async Task Liste_ExposeIngredientsEtAuteurPourLaRechercheEtLesFiltres()
    {
        var alice = await ConnecterAsync("alice");
        var bob = await ConnecterAsync("bob");
        string id = await CreerRecetteAsync(alice);

        JsonElement pourAlice = (await alice.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray()
            .Single(cocktail => cocktail.GetProperty("id").GetString() == id);
        JsonElement pourBob = (await bob.GetFromJsonAsync<JsonElement>("/api/cocktails", Jeton)).EnumerateArray()
            .Single(cocktail => cocktail.GetProperty("id").GetString() == id);

        Assert.Equal(["Gin"], pourAlice.GetProperty("ingredients").EnumerateArray().Select(nom => nom.GetString()));
        Assert.True(pourAlice.GetProperty("modifiable").GetBoolean());
        Assert.False(pourBob.GetProperty("modifiable").GetBoolean());
    }
}
