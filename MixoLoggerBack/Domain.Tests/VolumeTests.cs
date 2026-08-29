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
	// B3 — cf. docs/MVP.md §7
	// `Volume` est un record : l'égalité générée compare Value ET Unit, sans
	// conversion. Deux volumes physiquement identiques exprimés dans des unités
	// différentes sont donc considérés comme distincts, alors que les opérateurs
	// < et > convertissent correctement.
	// Retirer le Skip une fois l'étape B‑2 faite (normalisation en mL à la construction).
	// ------------------------------------------------------------------
	[Fact(Skip = "B3 — l'égalité de Volume ne convertit pas les unités (voir docs/MVP.md §7)")]
	public void Egalite_MemeVolumeUnitesDifferentes_SontEgaux()
	{
		Assert.Equal(new Volume(100, UniteVolume.Mililitre), new Volume(10, UniteVolume.Centilitre));
	}
}
