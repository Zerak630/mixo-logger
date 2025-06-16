using System.Collections.Concurrent;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;

namespace Infrastructure.Repositories;

public class CocktailRepository : ICocktailRepository
{
	// Simule un stockage en mémoire
	private static readonly ConcurrentDictionary<Guid, Cocktail> _store = new();

	public Task<Cocktail?> GetByIdAsync(Guid id)
	{
		_store.TryGetValue(id, out var cocktail);
		return Task.FromResult(cocktail);
	}

	// Méthode d'ajout pour tests/démo
	public Task AddAsync(Cocktail cocktail)
	{
		_store[cocktail.Id] = cocktail;
		return Task.CompletedTask;
	}

	public Task<IEnumerable<Cocktail>> GetAllAsync()
	{
		return Task.FromResult(_store.Values.AsEnumerable());
	}

	public Task<IEnumerable<Cocktail>> GetByIngredientId(Guid ingredientId)
	{
		var result = _store.Values.Where(c => c.Ingredients.Any(i => i.Ingredient.Id == ingredientId));
		return Task.FromResult(result);
	}

	public Task UpdateAsync(Cocktail cocktail)
	{
		throw new NotImplementedException();
	}
}
