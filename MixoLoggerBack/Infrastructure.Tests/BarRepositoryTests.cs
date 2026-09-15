using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.MyBar;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class BarRepositoryTests : BaseDeTest
{
    private readonly Guid _proprietaire = Guid.NewGuid();

    private Task<Bar> LireAsync(Guid? proprietaire = null) =>
        AvecAsync<IBarRepository, Bar>(depot => depot.GetForOwnerAsync(proprietaire ?? _proprietaire));

    private Task EnregistrerAsync(Bar bar) => AvecAsync<IBarRepository>(depot => depot.SaveAsync(bar));

    private Task<Ingredient> IngredientAsync(string nom) =>
        AvecAsync<IIngredientRepository, Ingredient>(depot => depot.GetOrCreateAsync(nom));

    [Fact]
    public async Task BarJamaisEnregistre_EstVide_EnVersion0_EtLireNeCreeRien()
    {
        Bar bar = await LireAsync();

        Assert.Empty(bar.Stock);
        Assert.Equal(0, bar.Version);
        Assert.Equal(_proprietaire, bar.OwnerId);
        Assert.Equal(0, await DansLaBaseAsync(db => db.Bars.CountAsync(b => b.ProprietaireId == _proprietaire, Jeton)));
    }

    [Fact]
    public async Task Enregistrement_PuisLecture_RestitueNiveauxEtVolumes()
    {
        Bar bar = await LireAsync();
        bar.AddIngredient(await IngredientAsync("Gin"), NiveauStock.PresqueFinie);
        bar.AddIngredient(await IngredientAsync("Rhum blanc"), new Volume(70, UniteVolume.Centilitre));
        bar.SetNiveau(await IngredientAsync("Rhum blanc"), NiveauStock.Entamee);

        await EnregistrerAsync(bar);
        Bar relu = await LireAsync();

        Assert.Equal(1, relu.Version);
        Assert.Equal(bar.Id, relu.Id);
        Assert.Equal(2, relu.Stock.Count);
        LigneStock gin = relu.Stock[new Ingredient("Gin")];
        LigneStock rhum = relu.Stock[new Ingredient("Rhum blanc")];
        Assert.Equal(NiveauStock.PresqueFinie, gin.Niveau);
        Assert.False(gin.SuiviPrecis);
        Assert.Equal(NiveauStock.Entamee, rhum.Niveau);
        Assert.Equal(70, rhum.Volume!.Value);
        Assert.Equal(UniteVolume.Centilitre, rhum.Volume.Unit);
    }

    [Fact]
    public async Task ChaqueEnregistrement_IncrementeLaVersion()
    {
        Bar bar = await LireAsync();
        bar.AddIngredient(await IngredientAsync("Gin"));
        await EnregistrerAsync(bar);

        for (int attendue = 2; attendue <= 4; attendue++)
        {
            Bar courant = await LireAsync();
            courant.SetNiveau(await IngredientAsync("Gin"), attendue % 2 == 0 ? NiveauStock.Entamee : NiveauStock.Pleine);
            await EnregistrerAsync(courant);
            Assert.Equal(attendue, (await LireAsync()).Version);
        }
    }

    [Fact]
    public async Task VersionPerimee_LeveUnConflit_EtNeModifieRien()
    {
        Bar initial = await LireAsync();
        initial.AddIngredient(await IngredientAsync("Gin"));
        await EnregistrerAsync(initial);

        Bar lecture1 = await LireAsync();
        Bar lecture2 = await LireAsync();
        lecture1.AddIngredient(await IngredientAsync("Vodka"));
        lecture2.AddIngredient(await IngredientAsync("Tequila"));

        await EnregistrerAsync(lecture1);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => EnregistrerAsync(lecture2));

        Bar final = await LireAsync();
        Assert.Equal(2, final.Version);
        Assert.Equal(["Gin", "Vodka"], final.Stock.Keys.Select(i => i.Name).Order());
    }

    [Fact]
    public async Task DeuxPremiersEnregistrementsSimultanes_LeSecondEstEnConflit()
    {
        Bar premier = await LireAsync();
        Bar second = await LireAsync();
        premier.AddIngredient(await IngredientAsync("Gin"));
        second.AddIngredient(await IngredientAsync("Vodka"));

        await EnregistrerAsync(premier);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => EnregistrerAsync(second));

        Assert.Equal(["Gin"], (await LireAsync()).Stock.Keys.Select(i => i.Name));
    }

    [Fact]
    public async Task LigneRetiree_DisparaitDeLaBase()
    {
        Bar bar = await LireAsync();
        bar.AddIngredient(await IngredientAsync("Gin"));
        bar.AddIngredient(await IngredientAsync("Vodka"));
        await EnregistrerAsync(bar);

        Bar courant = await LireAsync();
        courant.RemoveIngredient(new Ingredient("Gin"));
        await EnregistrerAsync(courant);

        Assert.Equal(["Vodka"], (await LireAsync()).Stock.Keys.Select(i => i.Name));
        Assert.Equal(1, await DansLaBaseAsync(db => db.LignesStock.CountAsync(l => l.ProprietaireId == _proprietaire, Jeton)));
    }

    [Fact]
    public async Task BarVide_ApresRetraitDeTout_ResteEnregistre()
    {
        Bar bar = await LireAsync();
        bar.AddIngredient(await IngredientAsync("Gin"));
        await EnregistrerAsync(bar);

        Bar courant = await LireAsync();
        courant.RemoveIngredient(new Ingredient("Gin"));
        await EnregistrerAsync(courant);

        Bar relu = await LireAsync();
        Assert.Empty(relu.Stock);
        // Version conservée : une sauvegarde partant de la version 0 serait refusée.
        Assert.Equal(2, relu.Version);
    }

    [Fact]
    public async Task LesBars_SontIsolesParProprietaire()
    {
        Guid autre = Guid.NewGuid();

        Bar mien = await LireAsync();
        mien.AddIngredient(await IngredientAsync("Gin"));
        await EnregistrerAsync(mien);

        Bar sien = await LireAsync(autre);
        sien.AddIngredient(await IngredientAsync("Vodka"));
        await EnregistrerAsync(sien);

        Assert.Equal(["Gin"], (await LireAsync()).Stock.Keys.Select(i => i.Name));
        Assert.Equal(["Vodka"], (await LireAsync(autre)).Stock.Keys.Select(i => i.Name));
    }

    [Fact]
    public async Task PreparationDeCocktail_VolumeDecrementePersiste()
    {
        Ingredient rhum = await IngredientAsync("Rhum blanc");
        Bar bar = await LireAsync();
        bar.AddIngredient(rhum, new Volume(20, UniteVolume.Centilitre));
        await EnregistrerAsync(bar);

        Cocktail recette = new("Shot", [Composant(rhum, 50, "mL")], EtapeRecette.FromOrderedList(["Servir"]));
        Bar courant = await LireAsync();
        courant.MakeCocktail(recette, 3);
        await EnregistrerAsync(courant);

        Assert.Equal(new Volume(50, UniteVolume.Mililitre), (await LireAsync()).Stock[rhum].Volume);
    }
}
