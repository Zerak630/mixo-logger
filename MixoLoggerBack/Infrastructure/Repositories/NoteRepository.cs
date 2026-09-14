using System.Collections.Concurrent;
using Domain.Interfaces.Repositories;
using Domain.Notes;

namespace Infrastructure.Repositories;

/// <summary>Notes en mémoire, une par couple cocktail / utilisateur.</summary>
public class NoteRepository : INoteRepository
{
    private static readonly ConcurrentDictionary<(Guid CocktailId, Guid UtilisateurId), Note> _notes = new();

    public Task DefinirAsync(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);

        _notes[(note.CocktailId, note.UtilisateurId)] = note;
        return Task.CompletedTask;
    }

    public Task<bool> RetirerAsync(Guid cocktailId, Guid utilisateurId) =>
        Task.FromResult(_notes.TryRemove((cocktailId, utilisateurId), out _));

    public Task<IReadOnlyList<Note>> GetByCocktailAsync(Guid cocktailId) =>
        Task.FromResult<IReadOnlyList<Note>>([.. _notes.Values.Where(note => note.CocktailId == cocktailId)]);

    public Task<ILookup<Guid, Note>> GetAllByCocktailAsync() =>
        Task.FromResult(_notes.Values.ToLookup(note => note.CocktailId));

    public Task RetirerPourCocktailAsync(Guid cocktailId)
    {
        foreach ((Guid, Guid) cle in _notes.Keys.Where(cle => cle.CocktailId == cocktailId))
            _notes.TryRemove(cle, out _);

        return Task.CompletedTask;
    }
}
