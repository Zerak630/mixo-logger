using Domain.Cocktails;
using Xunit;

namespace Domain.Tests;

public class IngredientTests
{
	[Theory]
	[InlineData("Rhum blanc", "rhum blanc")]
	[InlineData("  Rhum   Blanc  ", "rhum blanc")]
	[InlineData("Crème de Coco", "creme de coco")]
	[InlineData("Jus d'orange", "jus d orange")]
	[InlineData("CURAÇAO BLEU", "curacao bleu")]
	public void Normalize_RamèneLesVariantesÀLaMêmeClé(string saisie, string attendu)
	{
		Assert.Equal(attendu, IngredientName.Normalize(saisie));
	}

	[Fact]
	public void Ingredient_NomVide_Leve()
	{
		Assert.Throws<ArgumentException>(() => new Ingredient("   "));
	}

	[Fact]
	public void Ingredient_NomNull_Leve()
	{
		Assert.Throws<ArgumentNullException>(() => new Ingredient(null!));
	}

	[Fact]
	public void Egalite_MemeNomNormalise_SontEgaux()
	{
		Assert.Equal(new Ingredient("Crème de coco"), new Ingredient("CREME  DE COCO"));
	}

	[Fact]
	public void Egalite_NomsDifferents_NeSontPasEgaux()
	{
		Assert.NotEqual(new Ingredient("Rhum blanc"), new Ingredient("Rhum ambré"));
	}

	[Fact]
	public void GetHashCode_MemeNomNormalise_EstIdentique()
	{
		Assert.Equal(
			new Ingredient("Jus d'orange").GetHashCode(),
			new Ingredient("jus d orange").GetHashCode());
	}

	[Fact]
	public void Matches_ReconnaitLeNomEtLesAlias()
	{
		var rhum = new Ingredient("Rhum blanc", "rhum", "white rum");

		Assert.True(rhum.Matches("Rhum Blanc"));
		Assert.True(rhum.Matches("RHUM"));
		Assert.True(rhum.Matches("White Rum"));
		Assert.False(rhum.Matches("Gin"));
	}

	[Fact]
	public void Aliases_NIncluentPasLeNomCanonique()
	{
		var rhum = new Ingredient("Rhum blanc", "rhum blanc", "rhum");

		Assert.Equal(["rhum"], rhum.Aliases);
	}

	[Fact]
	public void Egalite_NeDependPasDesAlias()
	{
		// Les alias ne participent pas à l'égalité : l'inclure la rendrait non transitive.
		Assert.Equal(new Ingredient("Rhum blanc", "rhum"), new Ingredient("Rhum blanc"));
	}
}
