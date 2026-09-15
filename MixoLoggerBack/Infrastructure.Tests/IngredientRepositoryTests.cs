using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Xunit;

namespace Infrastructure.Tests;

public class IngredientRepositoryTests : BaseDeTest
{
    private Task<Ingredient?> ParNomAsync(string nom) => AvecAsync<IIngredientRepository, Ingredient?>(depot => depot.GetByNameAsync(nom));

    private Task<Ingredient> ObtenirOuCreerAsync(string nom) => AvecAsync<IIngredientRepository, Ingredient>(depot => depot.GetOrCreateAsync(nom));

    [Theory]
    [InlineData("Rhum blanc")]
    [InlineData("RHUM BLANC")]
    [InlineData("  rhum-blanc  ")]
    [InlineData("rhum")]
    [InlineData("White Rum")]
    [InlineData("Rhum agricole blanc")]
    public async Task ParNom_ResoutNomCanoniqueEtAlias_SansTenirCompteDeLaCasseNiDeLaPonctuation(string saisie)
    {
        Ingredient? trouve = await ParNomAsync(saisie);

        Assert.NotNull(trouve);
        Assert.Equal("Rhum blanc", trouve.Name);
    }

    [Fact]
    public async Task ParNom_AccentsIgnores()
    {
        Assert.Equal("Crème de coco", (await ParNomAsync("creme de COCO"))?.Name);
        Assert.Equal("Bière ginger", (await ParNomAsync("biere ginger"))?.Name);
    }

    [Theory]
    [InlineData("Liqueur inexistante")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public async Task ParNom_Inconnu_OuVide_RenvoieNull(string saisie)
    {
        Assert.Null(await ParNomAsync(saisie));
    }

    [Fact]
    public async Task ParNom_RenvoieLesAlias()
    {
        Ingredient? triple = await ParNomAsync("Cointreau");

        Assert.NotNull(triple);
        Assert.Equal("Triple sec", triple.Name);
        Assert.Contains("cointreau", triple.Aliases);
        Assert.Contains("curacao triple sec", triple.Aliases);
    }

    [Fact]
    public async Task ObtenirOuCreer_NomInconnu_CreeEtConserveEntreDeuxRequetes()
    {
        string nom = Unique("Liqueur de test");

        Ingredient cree = await ObtenirOuCreerAsync($"  {nom}  ");
        Ingredient relu = await ObtenirOuCreerAsync(nom.ToUpperInvariant());

        Assert.Equal(nom, cree.Name);
        Assert.Equal(cree.Id, relu.Id);
        Assert.Equal(cree.Id, (await AvecAsync<IIngredientRepository, Ingredient?>(depot => depot.GetByIdAsync(cree.Id)))?.Id);
    }

    [Fact]
    public async Task ObtenirOuCreer_Alias_RenvoieLeCanonique_SansRienCreer()
    {
        int avant = (await AvecAsync<IIngredientRepository, IEnumerable<Ingredient>>(depot => depot.GetAllAsync())).Count();

        Ingredient ingredient = await ObtenirOuCreerAsync("angostura");

        Assert.Equal("Bitters", ingredient.Name);
        Assert.Equal(avant, (await AvecAsync<IIngredientRepository, IEnumerable<Ingredient>>(depot => depot.GetAllAsync())).Count());
    }

    [Fact]
    public async Task ObtenirOuCreer_EnParallele_UnSeulIngredient()
    {
        string nom = Unique("Sirop concurrent");

        Ingredient[] resultats = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => ObtenirOuCreerAsync(nom), Jeton)));

        Assert.Single(resultats.Select(ingredient => ingredient.Id).Distinct());
    }

    [Fact]
    public async Task ParId_Inconnu_RenvoieNull()
    {
        Assert.Null(await AvecAsync<IIngredientRepository, Ingredient?>(depot => depot.GetByIdAsync(Guid.NewGuid())));
    }

    [Fact]
    public async Task Tous_ConserveIdentifiantsEtAlias()
    {
        List<Ingredient> tous = [.. await AvecAsync<IIngredientRepository, IEnumerable<Ingredient>>(depot => depot.GetAllAsync())];

        Assert.Equal(tous.Count, tous.Select(ingredient => ingredient.Id).Distinct().Count());
        Assert.Equal(["dark rum", "rhum brun", "rhum vieux"], tous.Single(i => i.Name == "Rhum ambré").Aliases.Order());
    }
}
