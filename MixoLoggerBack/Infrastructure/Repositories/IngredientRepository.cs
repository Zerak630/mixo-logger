using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Référentiel des ingrédients : un nom canonique et des alias (cf. docs/STRATEGIE.md §7).
/// Il ramène les libellés équivalents (« Rhum », « white rum ») vers un seul ingrédient, pour
/// que le stock du bar et les recettes parlent de la même chose.
/// </summary>
public class IngredientRepository(MixoLoggerDbContext db) : IIngredientRepository
{
    public async Task<Ingredient?> GetByIdAsync(Guid id) =>
        (await AvecAlias().FirstOrDefaultAsync(ingredient => ingredient.Id == id))?.VersDomaine();

    /// <summary>Le nom canonique d'abord, puis les alias ; accents, casse et ponctuation ignorés.</summary>
    public async Task<Ingredient?> GetByNameAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        string cle = IngredientName.Normalize(name);
        if (cle.Length == 0)
            return null;

        IngredientDonnees? trouve = await AvecAlias().FirstOrDefaultAsync(ingredient => ingredient.NomNormalise == cle)
            ?? await AvecAlias().FirstOrDefaultAsync(ingredient => ingredient.Alias.Any(alias => alias.Alias == cle));

        return trouve?.VersDomaine();
    }

    /// <summary>
    /// Un nom inconnu devient un nouvel ingrédient : le référentiel s'enrichit de ce que les
    /// utilisateurs saisissent réellement.
    /// </summary>
    public async Task<Ingredient> GetOrCreateAsync(string name)
    {
        if (await GetByNameAsync(name) is { } existant)
            return existant;

        Ingredient nouveau = new(name.Trim());

        try
        {
            db.Ingredients.Add(nouveau.VersDonnees());
            await db.SaveChangesAsync();
            return nouveau;
        }
        catch (DbUpdateException erreur) when (MixoLoggerDbContext.EstViolationUnicite(erreur))
        {
            // Une autre requête vient de créer le même ingrédient : c'est le sien qui fait foi.
            return await GetByNameAsync(name)
                ?? throw new InvalidOperationException($"L'ingrédient « {name} » n'a pas pu être enregistré.", erreur);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    public async Task<IEnumerable<Ingredient>> GetAllAsync() =>
        (await AvecAlias().ToListAsync())
            .Select(ingredient => ingredient.VersDomaine())
            // Tri en mémoire : la collation de SQLite ne connaît pas l'ordre alphabétique français.
            .OrderBy(ingredient => ingredient.Name, StringComparer.CurrentCulture)
            .ToList();

    private IQueryable<IngredientDonnees> AvecAlias() =>
        db.Ingredients.AsNoTracking().Include(ingredient => ingredient.Alias);
}
