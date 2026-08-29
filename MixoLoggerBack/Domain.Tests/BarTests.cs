using Domain.Cocktails;
using Domain.MyBar;
using Xunit;

namespace Domain.Tests;

public class BarTests
{
	private static Cocktail UnCocktailAvec(params CocktailIngredient[] ingredients) =>
		new("Cocktail de test", ingredients, EtapeRecette.FromOrderedList(["Verser"]));

	[Fact]
	public void AddIngredient_Null_Leve()
	{
		var bar = new Bar();

		Assert.Throws<ArgumentNullException>(() => bar.AddIngredient(null!, new Volume(50, UniteVolume.Mililitre)));
	}

	[Fact]
	public void AddIngredient_NouvelIngredient_CreeLaLigneDeStock()
	{
		var bar = new Bar();
		var rhum = new Ingredient("Rhum");

		bar.AddIngredient(rhum, new Volume(70, UniteVolume.Centilitre));

		// AddIngredient additionne systématiquement avec `Volume.Zero`, qui est exprimé
		// en millilitres : toute première entrée de stock est donc normalisée en mL,
		// quelle que soit l'unité de saisie. Comportement acceptable mais non documenté —
		// à confirmer lors de l'étape B‑2 (normalisation explicite de `Volume`).
		Assert.Equal(700d, bar.Ingredients[rhum].Value, precision: 10);
		Assert.Equal(UniteVolume.Mililitre, bar.Ingredients[rhum].Unit);
	}

	[Fact]
	public void AddIngredient_DeuxFois_CumuleLesVolumes()
	{
		var bar = new Bar();
		var rhum = new Ingredient("Rhum");

		bar.AddIngredient(rhum, new Volume(50, UniteVolume.Mililitre));
		bar.AddIngredient(rhum, new Volume(30, UniteVolume.Mililitre));

		Assert.Equal(80d, bar.Ingredients[rhum].Value, precision: 10);
	}

	[Fact]
	public void CanMake_Null_Leve()
	{
		Assert.Throws<ArgumentNullException>(() => new Bar().CanMake(null!));
	}

	[Fact]
	public void CanMake_StockSuffisant_RenvoieVrai()
	{
		var rhum = new Ingredient("Rhum");
		var bar = new Bar();
		bar.AddIngredient(rhum, new Volume(100, UniteVolume.Mililitre));

		var cocktail = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));

		Assert.True(bar.CanMake(cocktail));
	}

	[Fact]
	public void CanMake_StockInsuffisant_RenvoieFaux()
	{
		var rhum = new Ingredient("Rhum");
		var bar = new Bar();
		bar.AddIngredient(rhum, new Volume(20, UniteVolume.Mililitre));

		var cocktail = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));

		Assert.False(bar.CanMake(cocktail));
	}

	[Fact]
	public void CanMake_IngredientAbsent_RenvoieFaux()
	{
		var bar = new Bar();
		bar.AddIngredient(new Ingredient("Gin"), new Volume(500, UniteVolume.Mililitre));

		var cocktail = UnCocktailAvec(new CocktailIngredient(new Ingredient("Rhum"), 50, UniteVolume.Mililitre));

		Assert.False(bar.CanMake(cocktail));
	}

	[Fact]
	public void MakeCocktail_Null_Leve()
	{
		Assert.Throws<ArgumentNullException>(() => new Bar().MakeCocktail(null!));
	}

	[Fact]
	public void MakeCocktail_StockInsuffisant_Leve()
	{
		var rhum = new Ingredient("Rhum");
		var bar = new Bar();
		bar.AddIngredient(rhum, new Volume(10, UniteVolume.Mililitre));

		var cocktail = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));

		Assert.Throws<InvalidOperationException>(() => bar.MakeCocktail(cocktail));
	}

	[Fact]
	public void MakeCocktail_DecrementeLeStock()
	{
		var rhum = new Ingredient("Rhum");
		var bar = new Bar();
		bar.AddIngredient(rhum, new Volume(100, UniteVolume.Mililitre));

		bar.MakeCocktail(UnCocktailAvec(new CocktailIngredient(rhum, 40, UniteVolume.Mililitre)));

		Assert.Equal(60d, bar.Ingredients[rhum].Value, precision: 10);
	}

	// ------------------------------------------------------------------
	// B1 — cf. docs/MVP.md §7
	//
	// Les tests ci-dessus passent parce qu'ils partagent la MÊME instance
	// d'Ingredient entre le bar et le cocktail. Dans l'application réelle ce
	// n'est jamais le cas : `CocktailRepository` et `BarRepository` construisent
	// chacun leurs propres `new Ingredient("Rhum")`. Or `Ingredient` est une
	// class sans override d'Equals/GetHashCode et son constructeur génère un
	// Guid.NewGuid() : le Dictionary compare donc par référence et le
	// TryGetValue de CanMake échoue systématiquement.
	//
	// Conséquence : `CanMake` renvoie toujours false en production.
	// Retirer les Skip une fois l'étape B‑1 faite (identité par nom normalisé).
	// ------------------------------------------------------------------

	[Fact(Skip = "B1 — l'identité d'Ingredient est référentielle (voir docs/MVP.md §7)")]
	public void CanMake_MemeIngredientInstancesDifferentes_RenvoieVrai()
	{
		var bar = new Bar();
		bar.AddIngredient(new Ingredient("Rhum"), new Volume(100, UniteVolume.Mililitre));

		var cocktail = UnCocktailAvec(new CocktailIngredient(new Ingredient("Rhum"), 50, UniteVolume.Mililitre));

		Assert.True(bar.CanMake(cocktail));
	}

	[Fact(Skip = "B1 — l'identité d'Ingredient est référentielle (voir docs/MVP.md §7)")]
	public void AddIngredient_MemeNomInstancesDifferentes_CumuleAuLieuDeDupliquer()
	{
		var bar = new Bar();

		bar.AddIngredient(new Ingredient("Rhum"), new Volume(50, UniteVolume.Mililitre));
		bar.AddIngredient(new Ingredient("Rhum"), new Volume(30, UniteVolume.Mililitre));

		Assert.Single(bar.Ingredients);
		Assert.Equal(80d, bar.Ingredients.Single().Value.Value, precision: 10);
	}
}
