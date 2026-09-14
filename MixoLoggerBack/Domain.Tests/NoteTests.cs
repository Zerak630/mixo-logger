using Domain.Notes;
using Xunit;

namespace Domain.Tests;

public class NoteTests
{
    private static readonly Guid Cocktail = Guid.NewGuid();
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void HorsBornes_Leve(int valeur)
    {
        var erreur = Assert.Throws<ArgumentException>(() => new Note(Cocktail, Alice, valeur));
        Assert.Contains("entre 1 et 5", erreur.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Bornes_SontAcceptees(int valeur)
    {
        Assert.Equal(valeur, new Note(Cocktail, Alice, valeur).Valeur);
    }

    [Fact]
    public void SansCocktailOuSansUtilisateur_Leve()
    {
        Assert.Throws<ArgumentException>(() => new Note(Guid.Empty, Alice, 3));
        Assert.Throws<ArgumentException>(() => new Note(Cocktail, Guid.Empty, 3));
    }

    [Fact]
    public void Resume_SansNote()
    {
        var resume = ResumeNotes.Calculer([], Alice);

        Assert.Null(resume.Moyenne);
        Assert.Equal(0, resume.Nombre);
        Assert.Null(resume.MaNote);
    }

    [Fact]
    public void Resume_MoyenneDeTous_EtNoteDeLUtilisateur()
    {
        Note[] notes = [new(Cocktail, Alice, 5), new(Cocktail, Bob, 2)];

        var pourAlice = ResumeNotes.Calculer(notes, Alice);
        Assert.Equal(3.5, pourAlice.Moyenne);
        Assert.Equal(2, pourAlice.Nombre);
        Assert.Equal(5, pourAlice.MaNote);

        Assert.Null(ResumeNotes.Calculer(notes, Guid.NewGuid()).MaNote);
    }

    [Fact]
    public void Resume_MoyenneArrondieAuDixieme()
    {
        // 4 + 4 + 5 = 13 / 3 = 4,333…
        Note[] notes = [new(Cocktail, Alice, 4), new(Cocktail, Bob, 4), new(Cocktail, Guid.NewGuid(), 5)];

        Assert.Equal(4.3, ResumeNotes.Calculer(notes, Alice).Moyenne);
    }
}
