using Domain.Cocktails;
using Xunit;

namespace Domain.Tests;

public class VolumeTests
{
	[Fact]
	public void Constructeur_ValeurNegative_Leve()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(-1, UniteVolume.Mililitre));
	}

	[Fact]
	public void Zero_EstEnMillilitres()
	{
		Assert.Equal(0d, Volume.Zero.Value);
		Assert.Equal(UniteVolume.Mililitre, Volume.Zero.Unit);
	}

	[Fact]
	public void Addition_MemeUnite_ConserveLUnite()
	{
		var somme = new Volume(30, UniteVolume.Centilitre) + new Volume(20, UniteVolume.Centilitre);

		Assert.Equal(50d, somme.Value, precision: 10);
		Assert.Equal(UniteVolume.Centilitre, somme.Unit);
	}

	[Fact]
	public void Addition_UnitesDifferentes_BasculeEnMillilitres()
	{
		var somme = new Volume(5, UniteVolume.Centilitre) + new Volume(30, UniteVolume.Mililitre);

		Assert.Equal(80d, somme.Value, precision: 10);
		Assert.Equal(UniteVolume.Mililitre, somme.Unit);
	}

	[Fact]
	public void Soustraction_UnitesDifferentes_BasculeEnMillilitres()
	{
		var reste = new Volume(1, UniteVolume.Litre) - new Volume(25, UniteVolume.Centilitre);

		Assert.Equal(750d, reste.Value, precision: 10);
		Assert.Equal(UniteVolume.Mililitre, reste.Unit);
	}

	[Fact]
	public void Soustraction_ResultatNegatif_Leve()
	{
		Assert.Throws<ArgumentOutOfRangeException>(
			() => new Volume(10, UniteVolume.Mililitre) - new Volume(20, UniteVolume.Mililitre));
	}

	[Fact]
	public void Comparaison_UnitesDifferentes_ConvertitAvantDeComparer()
	{
		Assert.True(new Volume(30, UniteVolume.Mililitre) < new Volume(5, UniteVolume.Centilitre));
		Assert.True(new Volume(1, UniteVolume.Litre) > new Volume(90, UniteVolume.Centilitre));
	}

	// ------------------------------------------------------------------
	// B3 — l'égalité compare désormais en millilitres, comme le font déjà
	// les opérateurs < et > (cf. docs/MVP.md §7).
	// ------------------------------------------------------------------

	[Fact]
	public void Egalite_MemeVolumeUnitesDifferentes_SontEgaux()
	{
		Assert.Equal(new Volume(100, UniteVolume.Mililitre), new Volume(10, UniteVolume.Centilitre));
	}

	[Fact]
	public void Egalite_VolumesDifferents_NeSontPasEgaux()
	{
		Assert.NotEqual(new Volume(100, UniteVolume.Mililitre), new Volume(11, UniteVolume.Centilitre));
	}

	[Fact]
	public void GetHashCode_MemeVolumeUnitesDifferentes_EstIdentique()
	{
		// Indispensable : sans ça, deux volumes égaux tomberaient dans des
		// compartiments différents d'un Dictionary ou d'un HashSet.
		Assert.Equal(
			new Volume(1, UniteVolume.Litre).GetHashCode(),
			new Volume(10, UniteVolume.Decilitre).GetHashCode());
	}

	[Fact]
	public void Egalite_ToutesLesUnitesEquivalentes_SontEgales()
	{
		var litre = new Volume(1, UniteVolume.Litre);

		Assert.Equal(litre, new Volume(10, UniteVolume.Decilitre));
		Assert.Equal(litre, new Volume(100, UniteVolume.Centilitre));
		Assert.Equal(litre, new Volume(1000, UniteVolume.Mililitre));
	}

	[Fact]
	public void EnMillilitres_ConvertitDepuisChaqueUnite()
	{
		Assert.Equal(1000d, new Volume(1, UniteVolume.Litre).EnMillilitres, precision: 10);
		Assert.Equal(1000d, new Volume(10, UniteVolume.Decilitre).EnMillilitres, precision: 10);
		Assert.Equal(1000d, new Volume(100, UniteVolume.Centilitre).EnMillilitres, precision: 10);
		Assert.Equal(1000d, new Volume(1000, UniteVolume.Mililitre).EnMillilitres, precision: 10);
	}

	[Fact]
	public void Multiplication_ConserveLUnite()
	{
		var triple = new Volume(5, UniteVolume.Centilitre) * 3;

		Assert.Equal(15d, triple.Value, precision: 10);
		Assert.Equal(UniteVolume.Centilitre, triple.Unit);
	}

	[Fact]
	public void Multiplication_FacteurNegatif_Leve()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new Volume(5, UniteVolume.Centilitre) * -1);
	}
}
