using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.Notes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public class NoteRepositoryTests : BaseDeTest
{
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    private Task<Guid> MojitoAsync() => AvecAsync<ICocktailRepository, Guid>(async depot =>
        (await depot.GetAllAsync()).Single(c => c.Name == "Mojito").Id);

    private Task DefinirAsync(Note note) => AvecAsync<INoteRepository>(depot => depot.DefinirAsync(note));

    private Task<IReadOnlyList<Note>> NotesAsync(Guid cocktail) =>
        AvecAsync<INoteRepository, IReadOnlyList<Note>>(depot => depot.GetByCocktailAsync(cocktail));

    [Fact]
    public async Task Definir_PuisRelire_ConserveValeurEtDate()
    {
        Guid mojito = await MojitoAsync();
        Note note = Note.Reconstituer(mojito, _alice, 4, new DateTime(2026, 9, 1, 20, 30, 0, DateTimeKind.Utc));

        await DefinirAsync(note);

        Note relue = Assert.Single(await NotesAsync(mojito));
        Assert.Equal(4, relue.Valeur);
        Assert.Equal(_alice, relue.UtilisateurId);
        Assert.Equal(note.NoteeLe, relue.NoteeLe, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task DefinirANouveau_RemplaceSansDupliquer()
    {
        Guid mojito = await MojitoAsync();

        await DefinirAsync(new Note(mojito, _alice, 2));
        await DefinirAsync(new Note(mojito, _alice, 5));

        Note relue = Assert.Single(await NotesAsync(mojito));
        Assert.Equal(5, relue.Valeur);
    }

    [Fact]
    public async Task DefinirEnParallele_MemeUtilisateur_UneSeuleNote()
    {
        Guid mojito = await MojitoAsync();

        await Task.WhenAll(Enumerable.Range(1, 5).Select(valeur => Task.Run(() => DefinirAsync(new Note(mojito, _alice, valeur)), Jeton)));

        Assert.Single(await NotesAsync(mojito));
    }

    [Fact]
    public async Task Retirer_RenvoieVraiSeulementSiUneNoteExistait_EtNeToucheQueLaSienne()
    {
        Guid mojito = await MojitoAsync();
        await DefinirAsync(new Note(mojito, _alice, 3));
        await DefinirAsync(new Note(mojito, _bob, 4));

        Assert.True(await AvecAsync<INoteRepository, bool>(depot => depot.RetirerAsync(mojito, _alice)));
        Assert.False(await AvecAsync<INoteRepository, bool>(depot => depot.RetirerAsync(mojito, _alice)));

        Assert.Equal([_bob], (await NotesAsync(mojito)).Select(n => n.UtilisateurId));
    }

    [Fact]
    public async Task Definir_SurUnCocktailInexistant_LeveKeyNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => DefinirAsync(new Note(Guid.NewGuid(), _alice, 3)));

        Assert.Equal(0, await DansLaBaseAsync(db => db.Notes.CountAsync(Jeton)));
    }

    [Fact]
    public async Task ToutesParCocktail_RegroupeLesNotes()
    {
        List<Cocktail> cocktails = [.. await AvecAsync<ICocktailRepository, IEnumerable<Cocktail>>(depot => depot.GetAllAsync())];
        Guid mojito = cocktails.Single(c => c.Name == "Mojito").Id;
        Guid cosmo = cocktails.Single(c => c.Name == "Cosmopolitan").Id;

        await DefinirAsync(new Note(mojito, _alice, 5));
        await DefinirAsync(new Note(mojito, _bob, 3));
        await DefinirAsync(new Note(cosmo, _alice, 1));

        ILookup<Guid, Note> parCocktail = await AvecAsync<INoteRepository, ILookup<Guid, Note>>(depot => depot.GetAllByCocktailAsync());

        Assert.Equal(2, parCocktail[mojito].Count());
        Assert.Equal([1], parCocktail[cosmo].Select(n => n.Valeur));
        Assert.Empty(parCocktail[Guid.NewGuid()]);
    }

    [Fact]
    public async Task RetirerPourCocktail_NeToucheQueCeCocktail()
    {
        List<Cocktail> cocktails = [.. await AvecAsync<ICocktailRepository, IEnumerable<Cocktail>>(depot => depot.GetAllAsync())];
        Guid mojito = cocktails.Single(c => c.Name == "Mojito").Id;
        Guid cosmo = cocktails.Single(c => c.Name == "Cosmopolitan").Id;
        await DefinirAsync(new Note(mojito, _alice, 5));
        await DefinirAsync(new Note(cosmo, _alice, 2));

        await AvecAsync<INoteRepository>(depot => depot.RetirerPourCocktailAsync(mojito));

        Assert.Empty(await NotesAsync(mojito));
        Assert.Single(await NotesAsync(cosmo));
    }
}
