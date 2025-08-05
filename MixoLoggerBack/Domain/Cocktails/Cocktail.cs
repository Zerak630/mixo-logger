using Domain.Interfaces;

namespace Domain.Cocktails;

public class Cocktail : IAggregate
{
	public Guid Id { get; }
	public string Name { get; }
	public string? Description { get; }
	public IReadOnlyCollection<CocktailIngredient> Ingredients { get; }
	public IReadOnlyCollection<EtapeRecette> EtapeRecettes { get; }

	public Cocktail(string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description = null)
	{
		Id = Guid.NewGuid();
		Name = name
			?? throw new ArgumentNullException(nameof(name), "Cocktail name cannot be null");
		Description = description;

		if (ingredients == null || !ingredients.Any())
			throw new ArgumentException("Un cocktail doit avoir au moins un ingrédient.", nameof(ingredients));
		Ingredients = new List<CocktailIngredient>(ingredients).AsReadOnly();

		if (etapes == null || !etapes.Any())
			throw new ArgumentException("Un cocktail doit avoir au moins une étape.", nameof(ingredients));
		EtapeRecettes = new List<EtapeRecette>(etapes).AsReadOnly();
	}
}
