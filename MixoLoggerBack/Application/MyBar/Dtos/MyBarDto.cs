using Application.MyBar.Dtos;
using Domain.MyBar;

namespace Application.MyBar.Dtos;

public class MyBarDto
{
    public List<IngredientDto> Ingredients { get; init; }
    public MyBarDto(Bar bar)
    {
        if (bar == null) throw new ArgumentNullException(nameof(bar), "Bar cannot be null");

        Ingredients = bar.Ingredients
            .Select(i => new IngredientDto(i.Key, i.Value))
            .ToList();
    }
}