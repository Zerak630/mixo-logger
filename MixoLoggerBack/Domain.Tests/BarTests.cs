using Domain.Cocktails;
using Domain.MyBar;
using Xunit;

namespace Domain.Tests;

public class BarTests
{
	private static Cocktail UnCocktailAvec(params CocktailIngredient[] ingredients) =>
		new("Cocktail de test", ingredients, EtapeRecette.FromOrderedList(["Verser"]));

	private static Volume Ml(double value) => new(value, UniteVolume.Mililitre);

	// ------------------------------------------------------------------
	// Possession simple — mode nominal du grand public : on sait qu'on a
	// l'ingrédient, pas combien il en reste (cf. docs/STRATEGIE.md §3).
	// ------------------------------------------------------------------

	[Fact]
	public void AddIngredient_Null_Leve()
	{
		var bar = new Bar();

		Assert.Throws<ArgumentNullException>(() => bar.AddIngredient(null!, Ml(50)));
	}

	[Fact]
	public void AddIngredient_SansVolume_CreeUneLigneEnPossessionSimple()
	{
		var bar = new Bar();
		var rhum = new Ingredient("Rhum blanc");

		bar.AddIngredient(rhum);

		Assert.True(bar.Has(rhum));
		Assert.Equal(NiveauStock.Pleine, bar.Stock[rhum].Niveau);
		Assert.False(bar.Stock[rhum].SuiviPrecis);
		Assert.Null(bar.Stock[rhum].Volume);
	}

	[Fact]
	public void CanMake_PossessionSimple_NeBloqueJamaisSurLaQuantite()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum);

		var cocktail = UnCocktailAvec(new CocktailIngredient(rhum, 5, UniteVolume.Litre));

		Assert.True(bar.CanMake(cocktail));
	}

	[Fact]
	public void MakeCocktail_PossessionSimple_LaisseLaLigneIntacte()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, NiveauStock.Entamee);

		bar.MakeCocktail(UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre)));

		Assert.Equal(NiveauStock.Entamee, bar.Stock[rhum].Niveau);
		Assert.Null(bar.Stock[rhum].Volume);
	}

	[Fact]
	public void SetNiveau_IngredientAbsent_Leve()
	{
		var bar = new Bar();

		Assert.Throws<KeyNotFoundException>(() => bar.SetNiveau(new Ingredient("Gin"), NiveauStock.PresqueFinie));
	}

	[Fact]
	public void SetNiveau_ConserveLeVolumeSuivi()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(300));

		bar.SetNiveau(rhum, NiveauStock.PresqueFinie);

		Assert.Equal(NiveauStock.PresqueFinie, bar.Stock[rhum].Niveau);
		Assert.Equal(300d, bar.Stock[rhum].Volume!.Value, precision: 10);
	}

	[Fact]
	public void RemoveIngredient_RetireLaLigne_EtEstIdempotent()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum);

		Assert.True(bar.RemoveIngredient(rhum));
		Assert.False(bar.Has(rhum));
		Assert.False(bar.RemoveIngredient(rhum));
	}

	// ------------------------------------------------------------------
	// Suivi précis — optionnel, conservé pour ceux qui le veulent et pour
	// la piste B2B.
	// ------------------------------------------------------------------

	[Fact]
	public void AddIngredient_AvecVolume_ConserveLUniteDeSaisie()
	{
		var bar = new Bar();
		var rhum = new Ingredient("Rhum blanc");

		bar.AddIngredient(rhum, new Volume(70, UniteVolume.Centilitre));

		Assert.True(bar.Stock[rhum].SuiviPrecis);
		Assert.Equal(70d, bar.Stock[rhum].Volume!.Value, precision: 10);
		Assert.Equal(UniteVolume.Centilitre, bar.Stock[rhum].Volume!.Unit);
	}

	[Fact]
	public void AddIngredient_DeuxFois_CumuleLesVolumes()
	{
		var bar = new Bar();
		var rhum = new Ingredient("Rhum blanc");

		bar.AddIngredient(rhum, Ml(50));
		bar.AddIngredient(rhum, Ml(30));

		Assert.Single(bar.Stock);
		Assert.Equal(80d, bar.Stock[rhum].Volume!.Value, precision: 10);
	}

	[Fact]
	public void CanMake_Null_Leve()
	{
		Assert.Throws<ArgumentNullException>(() => new Bar().CanMake(null!));
	}

	[Fact]
	public void CanMake_StockSuffisant_RenvoieVrai()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(100));

		Assert.True(bar.CanMake(UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre))));
	}

	[Fact]
	public void CanMake_StockInsuffisant_RenvoieFaux()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(20));

		Assert.False(bar.CanMake(UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre))));
	}

	[Fact]
	public void CanMake_IngredientAbsent_RenvoieFaux()
	{
		var bar = new Bar();
		bar.AddIngredient(new Ingredient("Gin"), Ml(500));

		var cocktail = UnCocktailAvec(new CocktailIngredient(new Ingredient("Rhum blanc"), 50, UniteVolume.Mililitre));

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
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(10));

		var cocktail = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));

		Assert.Throws<InvalidOperationException>(() => bar.MakeCocktail(cocktail));
	}

	[Fact]
	public void MakeCocktail_DecrementeLeStock()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(100));

		bar.MakeCocktail(UnCocktailAvec(new CocktailIngredient(rhum, 40, UniteVolume.Mililitre)));

		Assert.Equal(60d, bar.Stock[rhum].Volume!.Value, precision: 10);
	}

	// ------------------------------------------------------------------
	// B1 — l'identité d'un Ingredient est son nom normalisé, pas sa
	// référence ni son Guid (cf. docs/MVP.md §7).
	// ------------------------------------------------------------------

	[Fact]
	public void CanMake_MemeIngredientInstancesDifferentes_RenvoieVrai()
	{
		var bar = new Bar();
		bar.AddIngredient(new Ingredient("Rhum blanc"), Ml(100));

		var cocktail = UnCocktailAvec(new CocktailIngredient(new Ingredient("Rhum blanc"), 50, UniteVolume.Mililitre));

		Assert.True(bar.CanMake(cocktail));
	}

	[Fact]
	public void AddIngredient_MemeNomInstancesDifferentes_CumuleAuLieuDeDupliquer()
	{
		var bar = new Bar();

		bar.AddIngredient(new Ingredient("Rhum blanc"), Ml(50));
		bar.AddIngredient(new Ingredient("Rhum blanc"), Ml(30));

		Assert.Single(bar.Stock);
		Assert.Equal(80d, bar.Stock.Single().Value.Volume!.Value, precision: 10);
	}

	[Fact]
	public void AddIngredient_CasseEtAccentsDifferents_DesigneLeMemeIngredient()
	{
		var bar = new Bar();

		bar.AddIngredient(new Ingredient("Crème de coco"), Ml(50));
		bar.AddIngredient(new Ingredient("CREME DE COCO"), Ml(30));

		Assert.Single(bar.Stock);
	}

	// ------------------------------------------------------------------
	// B11 / B12 — la quantité commandée est honorée, et une commande
	// infaisable ne consomme rien du tout (cf. docs/MVP.md §7).
	// ------------------------------------------------------------------

	[Fact]
	public void MakeCocktail_AvecQuantite_DecrementeAutantDeFois()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(200));

		bar.MakeCocktail(UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre)), quantite: 3);

		Assert.Equal(50d, bar.Stock[rhum].Volume!.Value, precision: 10);
	}

	[Fact]
	public void CanMake_QuantiteTropGrandePourLeStock_RenvoieFaux()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(80));

		var cocktail = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));

		Assert.True(bar.CanMake(cocktail));
		Assert.False(bar.CanMake(cocktail, quantite: 2));
	}

	[Fact]
	public void MakeCocktail_QuantiteInvalide_Leve()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(200));

		var cocktail = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));

		Assert.Throws<ArgumentOutOfRangeException>(() => bar.MakeCocktail(cocktail, quantite: 0));
	}

	[Fact]
	public void CanMakeAll_CumuleLesBesoinsEntreCocktailsDifferents()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(80));

		var mojito = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));
		var daiquiri = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));

		// Chacun passe isolément, mais pas les deux : c'est tout l'intérêt du cumul.
		Assert.True(bar.CanMake(mojito));
		Assert.True(bar.CanMake(daiquiri));
		Assert.False(bar.CanMakeAll([new CommandeCocktail(mojito), new CommandeCocktail(daiquiri)]));
	}

	[Fact]
	public void MakeCocktails_CommandeInfaisable_NeConsommeRien()
	{
		var rhum = new Ingredient("Rhum blanc");
		var gin = new Ingredient("Gin");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(100));
		bar.AddIngredient(gin, Ml(10));

		var faisable = UnCocktailAvec(new CocktailIngredient(rhum, 50, UniteVolume.Mililitre));
		var infaisable = UnCocktailAvec(new CocktailIngredient(gin, 50, UniteVolume.Mililitre));

		Assert.Throws<InvalidOperationException>(() =>
			bar.MakeCocktails([new CommandeCocktail(faisable), new CommandeCocktail(infaisable)]));

		// Le premier cocktail ne doit pas avoir été entamé.
		Assert.Equal(100d, bar.Stock[rhum].Volume!.Value, precision: 10);
		Assert.Equal(10d, bar.Stock[gin].Volume!.Value, precision: 10);
	}

	// ------------------------------------------------------------------
	// B2 — chaque requête travaille sur sa propre copie du bar, sinon deux
	// requêtes concurrentes mutent le même dictionnaire (cf. docs/MVP.md §7).
	// ------------------------------------------------------------------

	[Fact]
	public void Snapshot_CopieLeStockEtLIdentite()
	{
		var rhum = new Ingredient("Rhum blanc");
		var bar = new Bar { Version = 7 };
		bar.AddIngredient(rhum, Ml(100));

		var copie = bar.Snapshot();

		Assert.Equal(7, copie.Version);
		Assert.Equal(bar.Id, copie.Id);
		Assert.Equal(bar.CreatedAt, copie.CreatedAt);
		Assert.Equal(100d, copie.Stock[rhum].Volume!.Value, precision: 10);
	}

	[Fact]
	public void Snapshot_LesModificationsNeSePropagentPas()
	{
		var rhum = new Ingredient("Rhum blanc");
		var gin = new Ingredient("Gin");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(100));

		var copie = bar.Snapshot();
		copie.MakeCocktail(UnCocktailAvec(new CocktailIngredient(rhum, 40, UniteVolume.Mililitre)));
		copie.AddIngredient(gin);

		Assert.Equal(100d, bar.Stock[rhum].Volume!.Value, precision: 10);
		Assert.False(bar.Has(gin));
		Assert.Equal(60d, copie.Stock[rhum].Volume!.Value, precision: 10);
	}

	[Fact]
	public void MakeCocktails_CommandeFaisable_DecrementeToutesLesLignes()
	{
		var rhum = new Ingredient("Rhum blanc");
		var citron = new Ingredient("Citron vert");
		var bar = new Bar();
		bar.AddIngredient(rhum, Ml(200));
		bar.AddIngredient(citron, Ml(100));

		var mojito = UnCocktailAvec(
			new CocktailIngredient(rhum, 50, UniteVolume.Mililitre),
			new CocktailIngredient(citron, 20, UniteVolume.Mililitre));

		bar.MakeCocktails([new CommandeCocktail(mojito, 2)]);

		Assert.Equal(100d, bar.Stock[rhum].Volume!.Value, precision: 10);
		Assert.Equal(60d, bar.Stock[citron].Volume!.Value, precision: 10);
	}
}
