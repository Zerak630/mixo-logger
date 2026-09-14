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

		Assert.NotNull(ci.Volume);
		Assert.Equal(5d, ci.Volume.Value, precision: 10);
		Assert.Equal(UniteVolume.Centilitre, ci.Volume.Unit);
	}

	[Fact]
	public void Constructeur_Decompte_NaPasDeVolume()
	{
		var ci = new CocktailIngredient(new Ingredient("Menthe"), 6, Dose.Feuille);

		Assert.Null(ci.Volume);
		Assert.False(ci.Dose.EstUnVolume);
		Assert.Equal(6d, ci.Dose.Valeur, precision: 10);
	}
}

public class DoseTests
{
	[Theory]
	[InlineData("mL")]
	[InlineData("cL")]
	[InlineData("dL")]
	[InlineData("L")]
	public void UniteDeVolume_ExposeLeVolume(string unite)
	{
		var dose = new Dose(2, unite);

		Assert.True(dose.EstUnVolume);
		Assert.Equal(UniteVolume.FromString(unite), dose.Volume!.Unit);
	}

	[Theory]
	[InlineData(Dose.Piece)]
	[InlineData(Dose.Feuille)]
	[InlineData(Dose.Trait)]
	[InlineData(Dose.Pincee)]
	public void UniteDeDecompte_NaPasDeVolume(string unite)
	{
		Assert.Null(new Dose(2, unite).Volume);
	}

	[Theory]
	[InlineData("cuillère")]
	[InlineData("ML")]
	[InlineData("")]
	public void UniteInconnue_Leve(string unite)
	{
		Assert.Throws<ArgumentException>(() => new Dose(2, unite));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	public void ValeurInvalide_Leve(double valeur)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new Dose(valeur, Dose.Trait));
	}

	[Fact]
	public void Multiplication_ConserveLUnite()
	{
		var dose = new Dose(2, Dose.Trait) * 3;

		Assert.Equal(6d, dose.Valeur, precision: 10);
		Assert.Equal(Dose.Trait, dose.Unite);
	}
}

public class CocktailValidationTests
{
	private static CocktailIngredient Rhum() => new(new Ingredient("Rhum blanc"), 50, UniteVolume.Mililitre);

	private static IEnumerable<EtapeRecette> UneEtape() => EtapeRecette.FromOrderedList(["Verser"]);

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void NomVide_Leve(string nom)
	{
		Assert.Throws<ArgumentException>(() => new Cocktail(nom, [Rhum()], UneEtape()));
	}

	[Fact]
	public void NomEtDescription_SontNettoyes()
	{
		var cocktail = new Cocktail("  Mojito  ", [Rhum()], UneEtape(), "   ");

		Assert.Equal("Mojito", cocktail.Name);
		Assert.Null(cocktail.Description);
	}

	[Fact]
	public void IngredientEnDouble_Leve()
	{
		var erreur = Assert.Throws<ArgumentException>(() => new Cocktail(
			"Mojito",
			[Rhum(), new CocktailIngredient(new Ingredient("RHUM BLANC"), 2, UniteVolume.Centilitre)],
			UneEtape()));

		Assert.Contains("Rhum blanc", erreur.Message);
	}

	[Fact]
	public void EtapeVide_Leve()
	{
		Assert.Throws<ArgumentException>(() => new Cocktail("Mojito", [Rhum()], EtapeRecette.FromOrderedList(["Verser", "  "])));
	}

	[Fact]
	public void EtapesParesseuses_NeSontCreeesQuUneFois()
	{
		var cocktail = new Cocktail("Mojito", [Rhum()], EtapeRecette.FromOrderedList(["Verser", "Mélanger"]));

		// Relire la collection doit renvoyer les mêmes instances, pas des étapes recréées.
		Assert.Same(cocktail.EtapeRecettes.First(), cocktail.EtapeRecettes.First());
		Assert.Equal(new[] { "Verser", "Mélanger" }, cocktail.EtapeRecettes.Select(etape => etape.Description));
	}

	[Fact]
	public void Modifier_ConserveLIdentifiant_EtRemplaceLeContenu()
	{
		var original = new Cocktail("Mojito", [Rhum()], UneEtape(), "Frais");

		var modifie = original.Modifier(
			"Mojito royal",
			[Rhum(), new CocktailIngredient(new Ingredient("Champagne"), 5, UniteVolume.Centilitre)],
			EtapeRecette.FromOrderedList(["Verser", "Compléter au champagne"]));

		Assert.Equal(original.Id, modifie.Id);
		Assert.Equal("Mojito royal", modifie.Name);
		Assert.Null(modifie.Description);
		Assert.Equal(2, modifie.Ingredients.Count);
		Assert.Equal(2, modifie.EtapeRecettes.Count);

		// L'original est intact : le dépôt substitue l'instance d'un bloc.
		Assert.Equal("Mojito", original.Name);
		Assert.Single(original.Ingredients);
	}

	[Fact]
	public void Modifier_ContenuInvalide_Leve()
	{
		var original = new Cocktail("Mojito", [Rhum()], UneEtape());

		Assert.Throws<ArgumentException>(() => original.Modifier("Mojito", [], UneEtape()));
	}
}
