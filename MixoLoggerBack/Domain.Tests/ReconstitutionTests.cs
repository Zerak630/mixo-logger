using Domain.Cocktails;
using Domain.MyBar;
using Domain.Notes;
using Xunit;

namespace Domain.Tests;

/// <summary>Objets relus depuis le stockage : identité conservée, règles du domaine toujours appliquées.</summary>
public class ReconstitutionTests
{
    private static CocktailIngredient Rhum() => new(new Ingredient("Rhum blanc"), 50, UniteVolume.Mililitre);

    [Fact]
    public void Cocktail_ConserveIdentifiantEtAuteur()
    {
        Guid id = Guid.NewGuid();
        Guid auteur = Guid.NewGuid();

        var cocktail = Cocktail.Reconstituer(id, "Mojito", [Rhum()], EtapeRecette.FromOrderedList(["Verser"]), "Frais", auteur);

        Assert.Equal(id, cocktail.Id);
        Assert.Equal(auteur, cocktail.AuthorId);
        Assert.Equal("Frais", cocktail.Description);
    }

    [Fact]
    public void Cocktail_ContenuInvalide_Leve()
    {
        Assert.Throws<ArgumentException>(() => Cocktail.Reconstituer(Guid.NewGuid(), "Mojito", [], EtapeRecette.FromOrderedList(["Verser"]), null, null));
    }

    [Fact]
    public void Bar_ConserveVersionEtLignes()
    {
        Guid proprietaire = Guid.NewGuid();
        var rhum = new Ingredient("Rhum blanc");
        var gin = new Ingredient("Gin");

        var bar = Bar.Reconstituer(Guid.NewGuid(), proprietaire, DateTime.UnixEpoch, 7,
        [
            KeyValuePair.Create(rhum, new LigneStock(NiveauStock.Entamee, new Volume(35, UniteVolume.Centilitre))),
            KeyValuePair.Create(gin, new LigneStock(NiveauStock.PresqueFinie))
        ]);

        Assert.Equal(7, bar.Version);
        Assert.Equal(proprietaire, bar.OwnerId);
        Assert.Equal(DateTime.UnixEpoch, bar.CreatedAt);
        // Le niveau d'une ligne en suivi précis est conservé, là où AddIngredient le remettrait à « pleine ».
        Assert.Equal(NiveauStock.Entamee, bar.Stock[rhum].Niveau);
        Assert.Equal(new Volume(350, UniteVolume.Mililitre), bar.Stock[rhum].Volume);
        Assert.Equal(NiveauStock.PresqueFinie, bar.Stock[gin].Niveau);
    }

    [Fact]
    public void Note_ConserveSaDate()
    {
        var date = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(date, Note.Reconstituer(Guid.NewGuid(), Guid.NewGuid(), 4, date).NoteeLe);
        Assert.Throws<ArgumentException>(() => Note.Reconstituer(Guid.NewGuid(), Guid.NewGuid(), 9, date));
    }
}
