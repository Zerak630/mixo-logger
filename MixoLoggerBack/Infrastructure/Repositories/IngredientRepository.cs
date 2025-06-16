using System.Collections.Concurrent;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;

namespace Infrastructure.Repositories;

public class IngredientRepository : IIngredientRepository
{
	private static readonly ConcurrentDictionary<Guid, Ingredient> _store = new();

	public Task<Ingredient?> GetByIdAsync(Guid id)
	{
		_store.TryGetValue(id, out var ingredient);
		return Task.FromResult(ingredient);
	}

	// Méthode d'ajout pour tests/démo
	public Task AddAsync(Ingredient ingredient)
	{
		_store[ingredient.Id] = ingredient;
		return Task.CompletedTask;
	}
}
