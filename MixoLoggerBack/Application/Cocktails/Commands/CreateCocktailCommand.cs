using Application.Cocktails.Dtos;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;

namespace Application.Cocktails.Commands;

public class CreateCocktailCommand
{
	public required string Name { get; set; }
	public string? Description { get; set; }
	public List<CocktailIngredientDto> Ingredients { get; set; } = new();
}

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
