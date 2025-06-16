using Domain.Interfaces;

namespace Domain.Cocktails;

public class Cocktail : IAggregate
{
	public Guid Id { get; }
	public string Name { get; }
	public string? Description { get; }
	public IReadOnlyCollection<CocktailIngredient> Ingredients { get; }

	public Cocktail(string name, IEnumerable<CocktailIngredient> ingredients, string? description = null)
	{
		Id = Guid.NewGuid();
		Name = name ?? throw new ArgumentNullException(nameof(name), "Cocktail name cannot be null");
		Description = description;
		if (ingredients == null || !ingredients.Any())
			throw new ArgumentException("Un cocktail doit avoir au moins un ingrédient.", nameof(ingredients));
		Ingredients = new List<CocktailIngredient>(ingredients).AsReadOnly();
	}
}
