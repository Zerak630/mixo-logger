using System.Collections.Concurrent;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;

namespace Infrastructure.Repositories;

public class CocktailRepository : ICocktailRepository
{
	// Simule un stockage en mémoire avec une liste initiale de cocktails
	private static readonly List<Cocktail> DEFAULT_COCKTAILS = [
		new Cocktail("Mojito",
        [
            new(new Ingredient("Rum"), 50, UniteVolume.Mililitre),
			new(new Ingredient("Mint"), 10, UniteVolume.Mililitre),
			new(new Ingredient("Lime"), 20, UniteVolume.Mililitre),
			new(new Ingredient("Sugar"), 15, UniteVolume.Mililitre),
			new(new Ingredient("Soda Water"), 100, UniteVolume.Mililitre)
		], "A refreshing cocktail with mint and lime."),
		new Cocktail("Margarita",
        [
            new(new Ingredient("Tequila"), 50, UniteVolume.Mililitre),
			new(new Ingredient("Lime Juice"), 30, UniteVolume.Mililitre),
			new(new Ingredient("Triple Sec"), 20, UniteVolume.Mililitre)
		], "A classic cocktail with tequila and lime."),
		new Cocktail("Old Fashioned",
        [
            new(new Ingredient("Bourbon"), 50, UniteVolume.Mililitre),
			new(new Ingredient("Sugar Cube"), 1, UniteVolume.Mililitre),
			new(new Ingredient("Angostura Bitters"), 2, UniteVolume.Mililitre),
			new(new Ingredient("Orange Peel"), 1, UniteVolume.Mililitre)
		], "A timeless cocktail with whiskey and bitters."),
		new Cocktail("Cosmopolitan",
        [
            new(new Ingredient("Vodka"), 40, UniteVolume.Mililitre),
			new(new Ingredient("Triple Sec"), 20, UniteVolume.Mililitre),
			new(new Ingredient("Cranberry Juice"), 30, UniteVolume.Mililitre),
			new(new Ingredient("Lime Juice"), 10, UniteVolume.Mililitre)
		], "A stylish cocktail with vodka and cranberry.")
	];
	private static readonly ConcurrentDictionary<Guid, Cocktail> _store = new(
		DEFAULT_COCKTAILS.ToDictionary(c => c.Id)
	);

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
