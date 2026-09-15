using Domain.Cocktails;
using Domain.MyBar;
using Xunit;

namespace Domain.Tests;

public class LigneStockTests
{
    private static Volume Ml(double valeur) => new(valeur, UniteVolume.Mililitre);
    private static Volume Cl(double valeur) => new(valeur, UniteVolume.Centilitre);

    [Fact]
    public void SansNiveau_Leve()
    {
        Assert.Throws<ArgumentNullException>(() => new LigneStock(null!));
    }

    [Fact]
    public void PossessionSimple_CouvreToutBesoin()
    {
        var ligne = new LigneStock(NiveauStock.PresqueFinie);

        Assert.False(ligne.SuiviPrecis);
        Assert.True(ligne.Couvre(new Volume(10, UniteVolume.Litre)));
    }

    [Theory]
    [InlineData(49.9, true)]
    [InlineData(50, true)]
    [InlineData(50.1, false)]
    [InlineData(70, false)]
    public void SuiviPrecis_CouvreSeulementJusquAuVolumeDisponible(double requisEnMl, bool couvre)
    {
        Assert.Equal(couvre, new LigneStock(NiveauStock.Pleine, Cl(5)).Couvre(Ml(requisEnMl)));
    }

    [Fact]
    public void Retirer_SuiviPrecis_DecompteEnConvertissantLesUnites()
    {
        LigneStock apres = new LigneStock(NiveauStock.Entamee, Cl(70)).Retirer(Ml(50));

        Assert.Equal(Ml(650), apres.Volume);
        Assert.Equal(NiveauStock.Entamee, apres.Niveau);
    }

    [Fact]
    public void Retirer_ToutLeVolume_LaisseUneLigneAZero()
    {
        LigneStock apres = new LigneStock(NiveauStock.Pleine, Ml(50)).Retirer(Ml(50));

        Assert.True(apres.SuiviPrecis);
        Assert.Equal(Volume.Zero, apres.Volume);
    }

    [Fact]
    public void Retirer_Insuffisant_Leve_SansModifierLaLigne()
    {
        var ligne = new LigneStock(NiveauStock.Pleine, Ml(30));

        Assert.Throws<InvalidOperationException>(() => ligne.Retirer(Ml(31)));
        Assert.Equal(Ml(30), ligne.Volume);
    }

    [Fact]
    public void Retirer_PossessionSimple_NeChangeRien()
    {
        var ligne = new LigneStock(NiveauStock.Entamee);

        Assert.Same(ligne, ligne.Retirer(new Volume(1, UniteVolume.Litre)));
    }

    [Fact]
    public void Ajouter_APossessionSimple_PasseEnSuiviPrecis_EtRepasseAPleine()
    {
        LigneStock apres = new LigneStock(NiveauStock.PresqueFinie).Ajouter(Cl(70));

        Assert.True(apres.SuiviPrecis);
        Assert.Equal(Cl(70), apres.Volume);
        Assert.Equal(NiveauStock.Pleine, apres.Niveau);
    }

    [Fact]
    public void Ajouter_ASuiviPrecis_CumuleLesVolumes()
    {
        LigneStock apres = new LigneStock(NiveauStock.PresqueFinie, Cl(5)).Ajouter(Ml(700));

        Assert.Equal(Ml(750), apres.Volume);
        Assert.Equal(NiveauStock.Pleine, apres.Niveau);
    }

    [Fact]
    public void Operations_NeModifientJamaisLaLigneDOrigine()
    {
        var ligne = new LigneStock(NiveauStock.Entamee, Cl(10));

        ligne.Ajouter(Cl(10));
        ligne.Retirer(Cl(5));

        Assert.Equal(Cl(10), ligne.Volume);
        Assert.Equal(NiveauStock.Entamee, ligne.Niveau);
    }

    [Fact]
    public void ArgumentsNuls_Levent()
    {
        var ligne = new LigneStock(NiveauStock.Pleine, Cl(10));

        Assert.Throws<ArgumentNullException>(() => ligne.Couvre(null!));
        Assert.Throws<ArgumentNullException>(() => ligne.Retirer(null!));
        Assert.Throws<ArgumentNullException>(() => ligne.Ajouter(null!));
    }
}
