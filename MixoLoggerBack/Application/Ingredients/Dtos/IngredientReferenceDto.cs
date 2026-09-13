using Domain.Cocktails;

namespace Application.Ingredients.Dtos;

/// <summary>Entrée du référentiel, telle que proposée à l'autocomplétion.</summary>
public class IngredientReferenceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }

    /// <summary>Autres libellés reconnus, sous forme normalisée (sans accents, en minuscules).</summary>
    public IReadOnlyCollection<string> Aliases { get; init; }

    public IngredientReferenceDto(Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient, nameof(ingredient));

        Id = ingredient.Id;
        Name = ingredient.Name;
        Aliases = ingredient.Aliases;
    }
}
