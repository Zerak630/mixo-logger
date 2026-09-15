using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CocktailRepository(MixoLoggerDbContext db) : ICocktailRepository
{
	public async Task<Cocktail?> GetByIdAsync(Guid id) =>
		(await db.Cocktails.AvecContenu().FirstOrDefaultAsync(cocktail => cocktail.Id == id))?.VersDomaine();

	public async Task<IEnumerable<Cocktail>> GetAllAsync() =>
		(await db.Cocktails.AvecContenu().ToListAsync()).Select(cocktail => cocktail.VersDomaine()).ToList();

	public async Task<IEnumerable<Cocktail>> GetByIngredientId(Guid ingredientId) =>
		(await db.Cocktails.AvecContenu()
			.Where(cocktail => cocktail.Composants.Any(composant => composant.IngredientId == ingredientId))
			.ToListAsync())
		.Select(cocktail => cocktail.VersDomaine())
		.ToList();

	/// <exception cref="InvalidOperationException">Une recette porte déjà ce nom (contrainte d'unicité, donc atomique).</exception>
	public async Task AddAsync(Cocktail cocktail)
	{
		ArgumentNullException.ThrowIfNull(cocktail);

		try
		{
			db.Cocktails.Add(cocktail.VersDonnees());
			await db.SaveChangesAsync();
		}
		catch (DbUpdateException erreur) when (MixoLoggerDbContext.EstViolationUnicite(erreur))
		{
			throw NomDejaPris(cocktail, erreur);
		}
		finally
		{
			db.ChangeTracker.Clear();
		}
	}

	/// <summary>Remplace le contenu de la recette d'un bloc, dans une transaction.</summary>
	/// <remarks>
	/// Pas de contrôle de version : deux éditions simultanées de la même recette gardent la
	/// dernière. Acceptable à quelques utilisateurs, la recette n'ayant qu'un auteur.
	/// </remarks>
	public async Task UpdateAsync(Cocktail cocktail)
	{
		ArgumentNullException.ThrowIfNull(cocktail);

		await using var transaction = await db.Database.BeginTransactionAsync();

		try
		{
			CocktailDonnees donnees = cocktail.VersDonnees();

			int modifiees = await db.Cocktails
				.Where(existant => existant.Id == cocktail.Id)
				.ExecuteUpdateAsync(colonnes => colonnes
					.SetProperty(existant => existant.Nom, donnees.Nom)
					.SetProperty(existant => existant.NomNormalise, donnees.NomNormalise)
					.SetProperty(existant => existant.Description, donnees.Description));

			if (modifiees == 0)
				throw new KeyNotFoundException($"Cocktail with ID {cocktail.Id} not found.");

			// Lignes et étapes sont réécrites entièrement : plus simple et plus sûr qu'une
			// réconciliation ligne à ligne, et la recette n'en compte que quelques-unes.
			await db.Composants.Where(composant => composant.CocktailId == cocktail.Id).ExecuteDeleteAsync();
			await db.Etapes.Where(etape => etape.CocktailId == cocktail.Id).ExecuteDeleteAsync();

			db.Composants.AddRange(donnees.Composants);
			db.Etapes.AddRange(donnees.Etapes);
			await db.SaveChangesAsync();

			await transaction.CommitAsync();
		}
		catch (DbUpdateException erreur) when (MixoLoggerDbContext.EstViolationUnicite(erreur))
		{
			throw NomDejaPris(cocktail, erreur);
		}
		finally
		{
			db.ChangeTracker.Clear();
		}
	}

	/// <summary>Supprime la recette ; ses lignes, étapes et notes partent avec elle (suppression en cascade).</summary>
	public async Task DeleteAsync(Cocktail cocktail)
	{
		ArgumentNullException.ThrowIfNull(cocktail);

		if (await db.Cocktails.Where(existant => existant.Id == cocktail.Id).ExecuteDeleteAsync() == 0)
			throw new KeyNotFoundException($"Cocktail with ID {cocktail.Id} not found.");
	}

	private static InvalidOperationException NomDejaPris(Cocktail cocktail, Exception cause) =>
		new($"Une recette s'appelle déjà « {cocktail.Name} ».", cause);
}
