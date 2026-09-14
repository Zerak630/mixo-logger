using Domain.Cocktails;

namespace Domain.Interfaces.Repositories;

public interface ICocktailRepository
{
	Task<Cocktail?> GetByIdAsync(Guid id);
	// Tu peux ajouter d'autres méthodes comme Add, Update, Delete, etc.
	Task<IEnumerable<Cocktail>> GetAllAsync();
	Task<IEnumerable<Cocktail>> GetByIngredientId(Guid ingredientId);
	Task AddAsync(Cocktail cocktail);
	Task UpdateAsync(Cocktail cocktail);

	/// <summary>Retire la recette telle qu'elle a été lue ; échoue si elle a changé entre-temps.</summary>
	Task DeleteAsync(Cocktail cocktail);
}
