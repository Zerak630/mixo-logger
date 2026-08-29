using Domain.Cocktails;
using Xunit;

namespace Domain.Tests;

public class CocktailTests
{
	private static CocktailIngredient UnIngredient() =>
		new(new Ingredient("Rhum"), 50, UniteVolume.Mililitre);

	private static IEnumerable<EtapeRecette> DesEtapes() =>
		EtapeRecette.FromOrderedList(["Verser", "Mélanger"]);

	[Fact]
	public void Constructeur_NomNul_Leve()
	{
		Assert.Throws<ArgumentNullException>(
			() => new Cocktail(null!, [UnIngredient()], DesEtapes()));
	}

	[Fact]
	public void Constructeur_SansIngredient_Leve()
	{
		Assert.Throws<ArgumentException>(
			() => new Cocktail("Eau plate", [], DesEtapes()));
	}

	[Fact]
	public void Constructeur_SansEtape_Leve()
	{
		Assert.Throws<ArgumentException>(
			() => new Cocktail("Mojito", [UnIngredient()], []));
	}

	[Fact]
	public void Constructeur_DescriptionOptionnelle()
	{
		var cocktail = new Cocktail("Mojito", [UnIngredient()], DesEtapes());

		Assert.Null(cocktail.Description);
	}

	[Fact]
	public void Constructeur_RenseigneLesCollections()
	{
		var cocktail = new Cocktail("Mojito", [UnIngredient()], DesEtapes(), "Rafraîchissant");

		Assert.Equal("Mojito", cocktail.Name);
		Assert.Equal("Rafraîchissant", cocktail.Description);
		Assert.Single(cocktail.Ingredients);
		Assert.Equal(2, cocktail.EtapeRecettes.Count);
		Assert.NotEqual(Guid.Empty, cocktail.Id);
	}
}

public class CocktailIngredientTests
{
	[Fact]
	public void Constructeur_QuantiteNulle_Leve()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CocktailIngredient(new Ingredient("Rhum"), 0, UniteVolume.Mililitre));
	}

	[Fact]
	public void Constructeur_QuantiteNegative_Leve()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new CocktailIngredient(new Ingredient("Rhum"), -5, UniteVolume.Mililitre));
	}

	[Fact]
	public void Constructeur_IngredientNul_Leve()
	{
		Assert.Throws<ArgumentNullException>(
			() => new CocktailIngredient(null!, 50, UniteVolume.Mililitre));
	}

	[Fact]
	public void Constructeur_ConstruitLeVolume()
	{
		var ci = new CocktailIngredient(new Ingredient("Rhum"), 5, UniteVolume.Centilitre);

		Assert.Equal(5d, ci.Volume.Value, precision: 10);
		Assert.Equal(UniteVolume.Centilitre, ci.Volume.Unit);
	}
}
