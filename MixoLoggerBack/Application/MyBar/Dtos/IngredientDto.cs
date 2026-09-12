using Domain.Cocktails;
using Domain.MyBar;

namespace Application.MyBar.Dtos;

public class IngredientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }

    /// <summary>Niveau approximatif : c'est l'information que l'utilisateur tient réellement à jour.</summary>
    public string Niveau { get; init; }

    /// <summary>Volume exact, <c>null</c> en possession simple.</summary>
    public Volume? Quantity { get; init; }

    public bool SuiviPrecis { get; init; }

    public IngredientDto(Ingredient ingredient, LigneStock ligne)
    {
        ArgumentNullException.ThrowIfNull(ingredient, nameof(ingredient));
        ArgumentNullException.ThrowIfNull(ligne, nameof(ligne));

        Id = ingredient.Id;
        Name = ingredient.Name;
        Niveau = ligne.Niveau.ToString();
        Quantity = ligne.Volume;
        SuiviPrecis = ligne.SuiviPrecis;
    }
}
