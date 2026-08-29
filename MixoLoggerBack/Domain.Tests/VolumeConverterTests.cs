using Domain.Cocktails;
using Domain.Units;
using Xunit;

namespace Domain.Tests;

public class VolumeConverterTests
{
	[Fact]
	public void Convert_VersLaMemeUnite_RenvoieLaValeurInchangee()
	{
		Assert.Equal(42d, VolumeConverter.Convert(42d, UniteVolume.Centilitre, UniteVolume.Centilitre));
	}

	[Theory]
	[InlineData(1d, UniteVolume.cL, 10d)]
	[InlineData(1d, UniteVolume.dL, 100d)]
	[InlineData(1d, UniteVolume.L, 1000d)]
	[InlineData(2.5d, UniteVolume.cL, 25d)]
	public void Convert_VersMillilitres(double valeur, string unite, double attendu)
	{
		var resultat = VolumeConverter.Convert(valeur, UniteVolume.FromString(unite), UniteVolume.Mililitre);

		Assert.Equal(attendu, resultat, precision: 10);
	}

	[Theory]
	[InlineData(1000d, UniteVolume.L, 1d)]
	[InlineData(1000d, UniteVolume.dL, 10d)]
	[InlineData(1000d, UniteVolume.cL, 100d)]
	public void Convert_DepuisMillilitres(double valeurEnMl, string versUnite, double attendu)
	{
		var resultat = VolumeConverter.Convert(valeurEnMl, UniteVolume.Mililitre, UniteVolume.FromString(versUnite));

		Assert.Equal(attendu, resultat, precision: 10);
	}

	[Fact]
	public void Convert_EstReversible()
	{
		var aller = VolumeConverter.Convert(33d, UniteVolume.Centilitre, UniteVolume.Mililitre);
		var retour = VolumeConverter.Convert(aller, UniteVolume.Mililitre, UniteVolume.Centilitre);

		Assert.Equal(33d, retour, precision: 10);
	}

	[Fact]
	public void Convert_SurUnVolume_ConserveLUniteCible()
	{
		var resultat = VolumeConverter.Convert(new Volume(5, UniteVolume.Centilitre), UniteVolume.Mililitre);

		Assert.Equal(50d, resultat.Value, precision: 10);
		Assert.Equal(UniteVolume.Mililitre, resultat.Unit);
	}

	[Fact]
	public void FromString_UniteInconnue_Leve()
	{
		Assert.Throws<ArgumentException>(() => UniteVolume.FromString("gallon"));
	}

	[Fact]
	public void FromString_Null_Leve()
	{
		Assert.Throws<ArgumentException>(() => UniteVolume.FromString(null));
	}
}
