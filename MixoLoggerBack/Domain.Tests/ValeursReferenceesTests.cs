using System.Text.Json;
using Domain.Cocktails;
using Domain.MyBar;
using Xunit;

namespace Domain.Tests;

/// <summary>Niveaux de stock et unités : les chaînes échangées avec le front et stockées en base.</summary>
public class ValeursReferenceesTests
{
    [Theory]
    [InlineData("Pleine")]
    [InlineData("Entamee")]
    [InlineData("PresqueFinie")]
    public void NiveauStock_AllerRetourParSonNom(string nom)
    {
        Assert.Equal(nom, NiveauStock.FromString(nom).ToString());
        Assert.Same(NiveauStock.FromString(nom), NiveauStock.FromString(nom));
    }

    [Theory]
    [InlineData("pleine")]
    [InlineData("Entamée")]
    [InlineData("")]
    [InlineData(null)]
    public void NiveauStock_NomInconnu_Leve(string? nom)
    {
        // Sensible à la casse : c'est la valeur exacte stockée en base et envoyée par le front.
        Assert.Throws<ArgumentException>(() => NiveauStock.FromString(nom));
    }

    [Theory]
    [InlineData("mL")]
    [InlineData("cL")]
    [InlineData("dL")]
    [InlineData("L")]
    public void UniteVolume_AllerRetourParSonNom(string nom)
    {
        Assert.Equal(nom, UniteVolume.FromString(nom).ToString());
    }

    [Theory]
    [InlineData("ml")]
    [InlineData("CL")]
    [InlineData("oz")]
    public void UniteVolume_NomInconnu_Leve(string nom)
    {
        Assert.Throws<ArgumentException>(() => UniteVolume.FromString(nom));
    }

    [Fact]
    public void Json_NiveauEtUnite_SerialisesParLeurNom()
    {
        string json = JsonSerializer.Serialize(new { niveau = NiveauStock.Entamee, volume = new Volume(70, UniteVolume.Centilitre) });

        Assert.Equal("""{"niveau":"Entamee","volume":{"Value":70,"Unit":"cL"}}""", json);
    }

    [Fact]
    public void Json_Relecture()
    {
        Assert.Equal(NiveauStock.PresqueFinie, JsonSerializer.Deserialize<NiveauStock>("\"PresqueFinie\""));
        Assert.Equal(UniteVolume.Decilitre, JsonSerializer.Deserialize<UniteVolume>("\"dL\""));
        Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<UniteVolume>("\"gallon\""));
    }

    [Fact]
    public void Dose_ListeDesUnites_OrdreDeSaisie()
    {
        Assert.Equal(["mL", "cL", "dL", "L", "piece", "feuille", "trait", "pincee"], Dose.Unites);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Dose_ValeurNonFinie_Leve(double valeur)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dose(valeur, "cL"));
    }

    [Fact]
    public void Dose_Multiplication_ParMoinsDUnVerre_Leve()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dose(2, Dose.Trait) * 0);
        Assert.Equal(new Dose(6, Dose.Trait), new Dose(2, Dose.Trait) * 3);
    }

    [Fact]
    public void CommandeCocktail_QuantiteInferieureA1_Leve()
    {
        var cocktail = new Cocktail("Shot", [new CocktailIngredient(new Ingredient("Vodka"), 4, "cL")], EtapeRecette.FromOrderedList(["Servir"]));

        Assert.Throws<ArgumentOutOfRangeException>(() => new CommandeCocktail(cocktail, 0));
        Assert.Throws<ArgumentNullException>(() => new CommandeCocktail(null!));
    }
}
