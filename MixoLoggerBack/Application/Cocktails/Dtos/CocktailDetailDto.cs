using Application.Utilisateurs;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Domain.Notes;

namespace Application.Cocktails.Dtos;

/// <summary>
/// Détail d'une recette. Remplace l'entité de domaine que <c>GET /api/cocktails/{id}</c>
/// exposait telle quelle, champs internes compris (cf. docs/MVP.md §7, B5).
/// </summary>
public class CocktailDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<DoseIngredientDto> Ingredients { get; init; }

    /// <summary>Étapes dans l'ordre de préparation.</summary>
    public IReadOnlyList<EtapeDto> Etapes { get; init; }

    /// <summary>
    /// Nom affiché de l'auteur. <c>null</c> pour une recette d'origine, ou si le compte de
    /// l'auteur a été retiré.
    /// </summary>
    public string? Auteur { get; init; }

    /// <summary>Vrai si l'utilisateur connecté en est l'auteur, donc peut la modifier et la supprimer.</summary>
    public bool Modifiable { get; init; }

    public NotesDto Notes { get; init; }

    public CocktailDetailDto(Cocktail cocktail, string? auteur, bool modifiable, ResumeNotes notes)
    {
        ArgumentNullException.ThrowIfNull(notes, nameof(notes));
        ArgumentNullException.ThrowIfNull(cocktail, nameof(cocktail));

        Id = cocktail.Id;
        Name = cocktail.Name;
        Description = cocktail.Description;
        Ingredients = [.. cocktail.Ingredients.Select(composant => new DoseIngredientDto(composant))];
        Etapes = [.. cocktail.EtapeRecettes.OrderBy(etape => etape.Ordre).Select(etape => new EtapeDto(etape.Ordre, etape.Description))];
        Auteur = auteur;
        Modifiable = modifiable;
        Notes = new NotesDto(notes);
    }

    /// <summary>Le détail tel que le voit l'utilisateur connecté : auteur, droit de modification et notes résolus.</summary>
    public static async Task<CocktailDetailDto> PourAsync(
        Cocktail cocktail,
        IUtilisateurRepository utilisateurRepository,
        INoteRepository noteRepository,
        IUtilisateurCourant utilisateurCourant)
    {
        ArgumentNullException.ThrowIfNull(cocktail, nameof(cocktail));

        string? auteur = cocktail.AuthorId is { } authorId
            ? (await utilisateurRepository.GetByIdAsync(authorId))?.NomAffiche
            : null;

        ResumeNotes notes = ResumeNotes.Calculer(await noteRepository.GetByCocktailAsync(cocktail.Id), utilisateurCourant.Id);

        return new CocktailDetailDto(cocktail, auteur, cocktail.EstModifiablePar(utilisateurCourant.Id), notes);
    }
}

/// <summary>Un ingrédient de la recette et sa dose pour un verre.</summary>
public class DoseIngredientDto
{
    public Guid IngredientId { get; init; }
    public string Name { get; init; }
    public double Valeur { get; init; }

    /// <summary><c>mL</c>, <c>cL</c>, <c>dL</c>, <c>L</c>, ou une unité de décompte (cf. <see cref="Dose.UnitesDecompte"/>).</summary>
    public string Unite { get; init; }

    public DoseIngredientDto(CocktailIngredient composant)
    {
        ArgumentNullException.ThrowIfNull(composant, nameof(composant));

        IngredientId = composant.Ingredient.Id;
        Name = composant.Ingredient.Name;
        Valeur = composant.Dose.Valeur;
        Unite = composant.Dose.Unite;
    }
}

public record EtapeDto(int Ordre, string Description);
