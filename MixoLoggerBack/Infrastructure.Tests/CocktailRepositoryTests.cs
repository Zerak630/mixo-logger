using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.Notes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class CocktailRepositoryTests : BaseDeTest
{
    private static readonly Guid Auteur = Guid.NewGuid();

    private Task<Ingredient> IngredientAsync(string nom) =>
        AvecAsync<IIngredientRepository, Ingredient>(depot => depot.GetOrCreateAsync(nom));

    private Task<Cocktail?> LireAsync(Guid id) =>
        AvecAsync<ICocktailRepository, Cocktail?>(depot => depot.GetByIdAsync(id));

    private async Task<Cocktail> EnregistrerAsync(string? nom = null, Guid? auteur = null)
    {
        Ingredient gin = await IngredientAsync("Gin");
        Ingredient menthe = await IngredientAsync("Menthe");
        Ingredient tonic = await IngredientAsync("Tonic");

        Cocktail cocktail = new(
            nom ?? Unique("Recette"),
            [Composant(gin, 4.5, "cL"), Composant(menthe, 6, Dose.Feuille), Composant(tonic, 12, "cL")],
            EtapeRecette.FromOrderedList(["Verser le gin", "Ajouter la menthe", "Compléter au tonic"]),
            "Description de test",
            auteur ?? Auteur);

        await AvecAsync<ICocktailRepository>(depot => depot.AddAsync(cocktail));
        return cocktail;
    }

    [Fact]
    public async Task Ajout_PuisLecture_RestitueToutLeContenu()
    {
        Cocktail enregistre = await EnregistrerAsync();

        Cocktail? relu = await LireAsync(enregistre.Id);

        Assert.NotNull(relu);
        Assert.Equal(enregistre.Name, relu.Name);
        Assert.Equal("Description de test", relu.Description);
        Assert.Equal(Auteur, relu.AuthorId);
        // Ordre de saisie, doses en volume et en décompte.
        Assert.Equal(["Gin", "Menthe", "Tonic"], relu.Ingredients.Select(c => c.Ingredient.Name));
        Assert.Equal([4.5, 6, 12], relu.Ingredients.Select(c => c.Dose.Valeur));
        Assert.Equal(["cL", Dose.Feuille, "cL"], relu.Ingredients.Select(c => c.Dose.Unite));
        Assert.Null(relu.Ingredients.ElementAt(1).Volume);
        Assert.Equal(["Verser le gin", "Ajouter la menthe", "Compléter au tonic"], relu.EtapeRecettes.OrderBy(e => e.Ordre).Select(e => e.Description));
        // Les ingrédients relus portent leur identifiant du référentiel.
        Assert.Equal((await IngredientAsync("Gin")).Id, relu.Ingredients.First().Ingredient.Id);
    }

    [Fact]
    public async Task RecetteDOrigine_SansAuteur_RelueSansAuteur()
    {
        Cocktail mojito = (await AvecAsync<ICocktailRepository, IEnumerable<Cocktail>>(depot => depot.GetAllAsync())).Single(c => c.Name == "Mojito");

        Assert.Null(mojito.AuthorId);
        Assert.Equal(["Rhum blanc", "Menthe", "Citron vert", "Sirop de sucre", "Eau gazeuse"], mojito.Ingredients.Select(c => c.Ingredient.Name));
    }

    [Theory]
    [InlineData("Negroni")]
    [InlineData("NEGRONI")]
    [InlineData("  négroni ")]
    public async Task Ajout_NomDejaPris_AccentsEtCasseIgnores_Leve409(string doublon)
    {
        await EnregistrerAsync("Negroni");

        var erreur = await Assert.ThrowsAsync<InvalidOperationException>(() => EnregistrerAsync(doublon));

        Assert.Contains("s'appelle déjà", erreur.Message);
        Assert.Equal(1, await DansLaBaseAsync(db => db.Cocktails.CountAsync(c => c.NomNormalise == "negroni", Jeton)));
    }

    [Fact]
    public async Task Ajout_RefuseDansUneRequete_LaMemeRequetePeutEncoreEcrire()
    {
        await EnregistrerAsync("Spritz");
        Ingredient gin = await IngredientAsync("Gin");

        await using var portee = Portee();
        var depot = portee.ServiceProvider.GetService(typeof(ICocktailRepository)) as ICocktailRepository;

        await Assert.ThrowsAsync<InvalidOperationException>(() => depot!.AddAsync(
            new Cocktail("spritz", [Composant(gin, 4, "cL")], EtapeRecette.FromOrderedList(["Verser"]))));

        // Le suivi des modifications a été vidé : l'échec précédent ne pollue pas l'écriture suivante.
        string autre = Unique("Autre recette");
        await depot!.AddAsync(new Cocktail(autre, [Composant(gin, 4, "cL")], EtapeRecette.FromOrderedList(["Verser"])));
        Assert.NotNull((await depot.GetAllAsync()).SingleOrDefault(c => c.Name == autre));
    }

    [Fact]
    public async Task Modification_RemplaceLignesEtEtapes_ConserveIdentifiantEtAuteur()
    {
        Cocktail original = await EnregistrerAsync();
        Ingredient citron = await IngredientAsync("Citron");
        Ingredient gin = await IngredientAsync("Gin");

        Cocktail modifie = original.Modifier(
            original.Name + " revisité",
            [Composant(citron, 2, "cL"), Composant(gin, 5, "cL")],
            EtapeRecette.FromOrderedList(["Presser le citron"]),
            null);

        await AvecAsync<ICocktailRepository>(depot => depot.UpdateAsync(modifie));
        Cocktail? relu = await LireAsync(original.Id);

        Assert.NotNull(relu);
        Assert.Equal(original.Name + " revisité", relu.Name);
        Assert.Null(relu.Description);
        Assert.Equal(Auteur, relu.AuthorId);
        Assert.Equal(["Citron", "Gin"], relu.Ingredients.Select(c => c.Ingredient.Name));
        Assert.Equal(["Presser le citron"], relu.EtapeRecettes.Select(e => e.Description));
        // Aucune ligne de l'ancienne version ne subsiste en base.
        Assert.Equal(2, await DansLaBaseAsync(db => db.Composants.CountAsync(c => c.CocktailId == original.Id, Jeton)));
        Assert.Equal(1, await DansLaBaseAsync(db => db.Etapes.CountAsync(e => e.CocktailId == original.Id, Jeton)));
    }

    [Fact]
    public async Task Modification_VersUnNomDejaPris_Leve409_EtNeChangeRien()
    {
        await EnregistrerAsync("Martini");
        Cocktail autre = await EnregistrerAsync("Gibson");
        Ingredient citron = await IngredientAsync("Citron");

        Cocktail renomme = autre.Modifier("MARTINI", [Composant(citron, 1, "cL")], EtapeRecette.FromOrderedList(["Verser"]));

        await Assert.ThrowsAsync<InvalidOperationException>(() => AvecAsync<ICocktailRepository>(depot => depot.UpdateAsync(renomme)));

        // Transaction annulée : ni le nom, ni les lignes n'ont bougé.
        Cocktail? relu = await LireAsync(autre.Id);
        Assert.Equal("Gibson", relu?.Name);
        Assert.Equal(3, relu?.Ingredients.Count);
    }

    [Fact]
    public async Task Modification_RecetteInconnue_LeveKeyNotFound()
    {
        Ingredient gin = await IngredientAsync("Gin");
        Cocktail fantome = new(Unique("Fantôme"), [Composant(gin, 4, "cL")], EtapeRecette.FromOrderedList(["Verser"]));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => AvecAsync<ICocktailRepository>(depot => depot.UpdateAsync(fantome)));
    }

    [Fact]
    public async Task Suppression_RetireLignesEtapesEtNotes()
    {
        Cocktail cocktail = await EnregistrerAsync();
        await AvecAsync<INoteRepository>(depot => depot.DefinirAsync(new Note(cocktail.Id, Guid.NewGuid(), 4)));

        await AvecAsync<ICocktailRepository>(depot => depot.DeleteAsync(cocktail));

        Assert.Null(await LireAsync(cocktail.Id));
        Assert.Equal((0, 0, 0), await DansLaBaseAsync(async db => (
            await db.Composants.CountAsync(c => c.CocktailId == cocktail.Id, Jeton),
            await db.Etapes.CountAsync(e => e.CocktailId == cocktail.Id, Jeton),
            await db.Notes.CountAsync(n => n.CocktailId == cocktail.Id, Jeton))));
        // Les ingrédients, eux, restent dans le référentiel.
        Assert.NotNull(await AvecAsync<IIngredientRepository, Ingredient?>(depot => depot.GetByNameAsync("Gin")));
    }

    [Fact]
    public async Task Suppression_RecetteInconnue_LeveKeyNotFound()
    {
        Cocktail cocktail = await EnregistrerAsync();
        await AvecAsync<ICocktailRepository>(depot => depot.DeleteAsync(cocktail));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => AvecAsync<ICocktailRepository>(depot => depot.DeleteAsync(cocktail)));
    }

    [Fact]
    public async Task ParIngredient_RenvoieLesRecettesQuiLUtilisent()
    {
        Ingredient rare = await IngredientAsync(Unique("Ingrédient rare"));
        Cocktail avec = new(Unique("Avec"), [Composant(rare, 1, "cL")], EtapeRecette.FromOrderedList(["Verser"]));
        await AvecAsync<ICocktailRepository>(depot => depot.AddAsync(avec));
        await EnregistrerAsync();

        List<Cocktail> trouves = [.. await AvecAsync<ICocktailRepository, IEnumerable<Cocktail>>(depot => depot.GetByIngredientId(rare.Id))];

        Assert.Equal([avec.Id], trouves.Select(c => c.Id));
    }

    [Fact]
    public async Task ParId_Inconnu_RenvoieNull()
    {
        Assert.Null(await LireAsync(Guid.NewGuid()));
    }
}
