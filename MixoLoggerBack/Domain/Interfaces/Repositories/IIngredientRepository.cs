using Domain.Cocktails;

namespace Domain.Interfaces.Repositories;

public interface IIngredientRepository
{
	Task<Ingredient?> GetByIdAsync(Guid id);

	/// <summary>Résout un libellé libre (nom canonique ou alias) vers l'ingrédient du référentiel.</summary>
	Task<Ingredient?> GetByNameAsync(string name);

	/// <summary>Comme <see cref="GetByNameAsync"/>, mais crée l'ingrédient s'il est inconnu.</summary>
	Task<Ingredient> GetOrCreateAsync(string name);

	Task<IEnumerable<Ingredient>> GetAllAsync();
}
