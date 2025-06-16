using Application.Cocktails.Commands;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;

namespace Application.CommandHandlers
{
	// Ce fichier a été déplacé dans Application\Cocktails\Commands\CreateCocktailCommandHandler.cs
	public class CreateCocktailCommandHandler(
		ICocktailRepository cocktailRepository,
		IIngredientRepository ingredientRepository
	)
	{
		public async Task<Guid> Handle(CreateCocktailCommand command, CancellationToken cancellationToken = default)
		{
			// Récupère les ingrédients depuis le repository
			var ingredients = await Task.WhenAll(command.Ingredients.Select(async dto =>
			{
				Ingredient ingredient = await ingredientRepository.GetByIdAsync(dto.IngredientId)
					?? throw new Exception($"Ingrédient introuvable: {dto.IngredientId}");

				return new CocktailIngredient(ingredient, dto.Quantity, UniteVolume.FromString(dto.Unit));
			}));

			var cocktail = new Cocktail(command.Name, ingredients, command.Description);
			await cocktailRepository.AddAsync(cocktail);
			return cocktail.Id;
		}
	}
}
