using Domain.Interfaces.Repositories;
using Domain.Notes;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>Notes des cocktails, une par couple cocktail / utilisateur (clé primaire).</summary>
public class NoteRepository(MixoLoggerDbContext db) : INoteRepository
{
    /// <exception cref="KeyNotFoundException">Le cocktail a été supprimé entre-temps.</exception>
    public async Task DefinirAsync(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);

        try
        {
            if (!await RemplacerAsync(note))
            {
                db.Notes.Add(new NoteDonnees
                {
                    CocktailId = note.CocktailId,
                    UtilisateurId = note.UtilisateurId,
                    Valeur = note.Valeur,
                    NoteeLe = note.NoteeLe
                });
                await db.SaveChangesAsync();
            }
        }
        catch (DbUpdateException erreur) when (MixoLoggerDbContext.EstViolationUnicite(erreur))
        {
            // Deux premières notes simultanées du même utilisateur : la seconde remplace la première.
            db.ChangeTracker.Clear();
            await RemplacerAsync(note);
        }
        catch (DbUpdateException erreur) when (MixoLoggerDbContext.EstViolationCleEtrangere(erreur))
        {
            throw new KeyNotFoundException($"Cocktail with ID {note.CocktailId} not found.", erreur);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    public async Task<bool> RetirerAsync(Guid cocktailId, Guid utilisateurId) =>
        await db.Notes
            .Where(note => note.CocktailId == cocktailId && note.UtilisateurId == utilisateurId)
            .ExecuteDeleteAsync() > 0;

    public async Task<IReadOnlyList<Note>> GetByCocktailAsync(Guid cocktailId) =>
        [.. (await db.Notes.AsNoTracking().Where(note => note.CocktailId == cocktailId).ToListAsync())
            .Select(note => note.VersDomaine())];

    public async Task<ILookup<Guid, Note>> GetAllByCocktailAsync() =>
        (await db.Notes.AsNoTracking().ToListAsync())
            .Select(note => note.VersDomaine())
            .ToLookup(note => note.CocktailId);

    /// <summary>La base supprime déjà les notes avec leur recette (cascade) ; conservé pour les appelants.</summary>
    public async Task RetirerPourCocktailAsync(Guid cocktailId) =>
        await db.Notes.Where(note => note.CocktailId == cocktailId).ExecuteDeleteAsync();

    private async Task<bool> RemplacerAsync(Note note) =>
        await db.Notes
            .Where(existante => existante.CocktailId == note.CocktailId && existante.UtilisateurId == note.UtilisateurId)
            .ExecuteUpdateAsync(colonnes => colonnes
                .SetProperty(existante => existante.Valeur, note.Valeur)
                .SetProperty(existante => existante.NoteeLe, note.NoteeLe)) > 0;
}
