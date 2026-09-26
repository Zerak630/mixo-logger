using Domain.Interfaces.Repositories;
using Domain.Utilisateurs;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>Comptes en base. Créés depuis la configuration par <c>SynchroniserComptesAsync</c>.</summary>
public class UtilisateurRepository(MixoLoggerDbContext db) : IUtilisateurRepository
{
    public async Task<Utilisateur?> GetByIdentifiantAsync(string identifiant)
    {
        string normalise = Utilisateur.Normaliser(identifiant ?? string.Empty);
        if (normalise.Length == 0)
            return null;

        return (await db.Comptes.AsNoTracking().FirstOrDefaultAsync(compte => compte.IdentifiantNormalise == normalise && compte.Actif))?.VersDomaine();
    }

    public async Task<Utilisateur?> GetByIdAsync(Guid id) =>
        (await db.Comptes.AsNoTracking().FirstOrDefaultAsync(compte => compte.Id == id && compte.Actif))?.VersDomaine();

    public async Task MettreAJourAsync(Utilisateur utilisateur)
    {
        ArgumentNullException.ThrowIfNull(utilisateur);

        try
        {
            CompteDonnees compte = await db.Comptes.FirstOrDefaultAsync(c => c.Id == utilisateur.Id)
                ?? throw new KeyNotFoundException($"Compte {utilisateur.Id} introuvable.");

            compte.Appliquer(utilisateur);
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException erreur) when (MixoLoggerDbContext.EstViolationUnicite(erreur))
        {
            throw new InvalidOperationException($"L'identifiant « {utilisateur.Identifiant} » est déjà pris.", erreur);
        }
        finally
        {
            db.ChangeTracker.Clear();
        }
    }
}
