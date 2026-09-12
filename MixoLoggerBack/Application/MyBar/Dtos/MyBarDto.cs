using Domain.MyBar;

namespace Application.MyBar.Dtos;

public class MyBarDto
{
    public List<IngredientDto> Ingredients { get; init; }

    public MyBarDto(Bar bar)
    {
        ArgumentNullException.ThrowIfNull(bar, nameof(bar));

        Ingredients = [.. bar.Stock
            .Select(ligne => new IngredientDto(ligne.Key, ligne.Value))
            .OrderBy(ingredient => ingredient.Name, StringComparer.CurrentCulture)];
    }
}
