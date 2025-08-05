using Domain.Cocktails;

namespace Application.MyBar.Dtos;

public class IngredientDto
{
    public string Name { get; init; }
    public Volume Quantity { get; init; }

    public IngredientDto(Ingredient ingredient, Volume quantity)
    {
        Name = ingredient?.Name ?? throw new ArgumentNullException(nameof(ingredient), "Ingredient cannot be null");
        Quantity = quantity ?? throw new ArgumentNullException(nameof(quantity), "Quantity cannot be null");
    }
}