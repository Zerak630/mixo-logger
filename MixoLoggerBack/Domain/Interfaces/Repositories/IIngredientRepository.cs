using Domain.Cocktails;

namespace Domain.Interfaces.Repositories;

public interface IIngredientRepository
{
	Task<Ingredient?> GetByIdAsync(Guid id);
	// Autres méthodes possibles : Add, Update, Delete, etc.
}
