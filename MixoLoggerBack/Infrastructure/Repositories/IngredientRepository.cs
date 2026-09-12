using Domain.Cocktails;
using Domain.Interfaces.Repositories;

namespace Infrastructure.Repositories;

public class IngredientRepository : IIngredientRepository
{
	public Task<Ingredient?> GetByIdAsync(Guid id) =>
		Task.FromResult(IngredientReferentiel.FindById(id));

	public Task<Ingredient?> GetByNameAsync(string name) =>
		Task.FromResult(IngredientReferentiel.Find(name));

	public Task<Ingredient> GetOrCreateAsync(string name) =>
		Task.FromResult(IngredientReferentiel.Resolve(name));

	public Task<IEnumerable<Ingredient>> GetAllAsync() =>
		Task.FromResult(IngredientReferentiel.All);
}
