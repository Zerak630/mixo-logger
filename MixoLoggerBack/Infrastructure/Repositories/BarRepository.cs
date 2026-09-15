using Domain.Interfaces.Repositories;
using Domain.MyBar;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>Un bar par utilisateur, indexé par <see cref="Bar.OwnerId"/>.</summary>
public class BarRepository(MixoLoggerDbContext db) : IBarRepository
{
    /// <summary>
    /// Renvoie une copie de travail, que la requête modifie puis publie via <see cref="SaveAsync"/>
    /// (cf. docs/MVP.md §7, B2). Consulter son bar ne crée rien : un bar jamais enregistré
    /// est rendu vide, en version 0.
    /// </summary>
    public async Task<Bar> GetForOwnerAsync(Guid ownerId)
    {
        BarDonnees? donnees = await db.Bars
            .AsNoTracking()
            .Include(bar => bar.Lignes).ThenInclude(ligne => ligne.Ingredient).ThenInclude(ingredient => ingredient!.Alias)
            .AsSplitQuery()
            .FirstOrDefaultAsync(bar => bar.ProprietaireId == ownerId);

        return donnees?.VersDomaine() ?? new Bar(ownerId);
    }

    /// <summary>
    /// Publie le bar, à condition qu'il ait été lu dans la version actuellement stockée. Sinon
    /// une autre requête a écrit entre-temps : on refuse plutôt que d'écraser sa modification.
    /// </summary>
    /// <remarks>
    /// Le passage à la version suivante est conditionné par la version lue, dans la même
    /// transaction que la réécriture des lignes : deux sauvegardes concurrentes ne peuvent
    /// pas réussir toutes les deux.
    /// </remarks>
    /// <exception cref="ConflitDeConcurrenceException">La version lue est périmée.</exception>
    public async Task SaveAsync(Bar bar)
    {
        ArgumentNullException.ThrowIfNull(bar);

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            if (bar.Version == 0)
            {
                // Premier enregistrement : la clé primaire (le propriétaire) départage deux créations simultanées.
                db.Bars.Add(new BarDonnees
                {
                    ProprietaireId = bar.OwnerId,
                    Id = bar.Id,
                    CreeLe = bar.CreatedAt,
                    Version = 1,
                    Lignes = bar.LignesVersDonnees()
                });
            }
            else
            {
                int publiees = await db.Bars
                    .Where(stocke => stocke.ProprietaireId == bar.OwnerId && stocke.Version == bar.Version)
                    .ExecuteUpdateAsync(colonnes => colonnes.SetProperty(stocke => stocke.Version, stocke => stocke.Version + 1));

                if (publiees == 0)
                    throw Conflit();

                await db.LignesStock.Where(ligne => ligne.ProprietaireId == bar.OwnerId).ExecuteDeleteAsync();
                db.LignesStock.AddRange(bar.LignesVersDonnees());
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException erreur) when (MixoLoggerDbContext.EstViolationUnicite(erreur))
        {
            throw Conflit(erreur);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }

    private static ConflitDeConcurrenceException Conflit(Exception? cause = null) =>
        new("Le bar a été modifié entre-temps. Recharge-le puis recommence.", cause);
}
