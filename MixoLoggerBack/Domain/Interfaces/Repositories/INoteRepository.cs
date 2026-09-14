using Domain.Notes;

namespace Domain.Interfaces.Repositories;

public interface INoteRepository
{
    /// <summary>Enregistre la note, en remplaçant celle que cet utilisateur avait déjà donnée à ce cocktail.</summary>
    Task DefinirAsync(Note note);

    /// <summary>Retire la note de cet utilisateur. Faux s'il n'en avait pas.</summary>
    Task<bool> RetirerAsync(Guid cocktailId, Guid utilisateurId);

    Task<IReadOnlyList<Note>> GetByCocktailAsync(Guid cocktailId);

    /// <summary>Toutes les notes, regroupées par cocktail : une seule lecture pour toute la liste.</summary>
    Task<ILookup<Guid, Note>> GetAllByCocktailAsync();

    /// <summary>Retire toutes les notes d'un cocktail supprimé.</summary>
    Task RetirerPourCocktailAsync(Guid cocktailId);
}
