using Domain.Cocktails;
using Domain.MyBar;

namespace Application.Cocktails.Dtos;

/// <summary>
/// Un cocktail dans la liste, avec ce que le bar courant permet d'en faire (F4).
/// Remplace l'entité de domaine que l'endpoint exposait jusque-là (cf. docs/MVP.md §7, B5).
/// </summary>
public class CocktailResumeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }

    /// <summary>Vrai si le bar contient de quoi préparer un verre.</summary>
    public bool Realisable { get; init; }

    /// <summary>Ce qui manque pour un verre, dans l'ordre de la recette. Vide si réalisable.</summary>
    public IReadOnlyList<ManqueDto> Manques { get; init; }

    public CocktailResumeDto(Cocktail cocktail, IReadOnlyList<Manque> manques)
    {
        ArgumentNullException.ThrowIfNull(cocktail, nameof(cocktail));
        ArgumentNullException.ThrowIfNull(manques, nameof(manques));

        Id = cocktail.Id;
        Name = cocktail.Name;
        Description = cocktail.Description;
        Manques = [.. manques.Select(manque => new ManqueDto(manque))];
        Realisable = Manques.Count == 0;
    }
}

public class ManqueDto
{
    public string Ingredient { get; init; }

    /// <summary><c>Absent</c> ou <c>Insuffisant</c>.</summary>
    public string Raison { get; init; }

    public ManqueDto(Manque manque)
    {
        ArgumentNullException.ThrowIfNull(manque, nameof(manque));

        Ingredient = manque.Ingredient.Name;
        Raison = manque.Raison.ToString();
    }
}
