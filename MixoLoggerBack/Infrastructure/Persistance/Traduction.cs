using Domain.Cocktails;
using Domain.MyBar;
using Domain.Notes;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance;

/// <summary>Passage entre les objets du domaine et leurs modèles de stockage.</summary>
internal static class Traduction
{
    public static Ingredient VersDomaine(this IngredientDonnees donnees) =>
        new(donnees.Nom, [.. donnees.Alias.Select(alias => alias.Alias)])
        {
            Id = donnees.Id,
            CreatedAt = donnees.CreeLe
        };

    public static IngredientDonnees VersDonnees(this Ingredient ingredient) => new()
    {
        Id = ingredient.Id,
        Nom = ingredient.Name,
        NomNormalise = ingredient.NormalizedName,
        CreeLe = ingredient.CreatedAt,
        Alias = [.. ingredient.Aliases.Select(alias => new AliasDonnees { Alias = alias, IngredientId = ingredient.Id })]
    };

    public static Cocktail VersDomaine(this CocktailDonnees donnees) =>
        Cocktail.Reconstituer(
            donnees.Id,
            donnees.Nom,
            donnees.Composants
                .OrderBy(composant => composant.Position)
                .Select(composant => new CocktailIngredient(composant.Ingredient!.VersDomaine(), new Dose(composant.Valeur, composant.Unite))),
            donnees.Etapes.Select(etape => new EtapeRecette(etape.Description, etape.Ordre)),
            donnees.Description,
            donnees.AuteurId);

    /// <summary>La recette et ses lignes. Les ingrédients ne sont référencés que par leur identifiant : ils doivent déjà exister.</summary>
    public static CocktailDonnees VersDonnees(this Cocktail cocktail) => new()
    {
        Id = cocktail.Id,
        Nom = cocktail.Name,
        NomNormalise = IngredientName.Normalize(cocktail.Name),
        Description = cocktail.Description,
        AuteurId = cocktail.AuthorId,
        Composants = [.. cocktail.Ingredients.Select((composant, position) => new ComposantDonnees
        {
            CocktailId = cocktail.Id,
            IngredientId = composant.Ingredient.Id,
            Position = position,
            Valeur = composant.Dose.Valeur,
            Unite = composant.Dose.Unite
        })],
        Etapes = [.. cocktail.EtapeRecettes.Select(etape => new EtapeDonnees
        {
            CocktailId = cocktail.Id,
            Ordre = etape.Ordre,
            Description = etape.Description
        })]
    };

    public static Bar VersDomaine(this BarDonnees donnees) =>
        Bar.Reconstituer(
            donnees.Id,
            donnees.ProprietaireId,
            donnees.CreeLe,
            donnees.Version,
            donnees.Lignes.Select(ligne => KeyValuePair.Create(
                ligne.Ingredient!.VersDomaine(),
                new LigneStock(
                    NiveauStock.FromString(ligne.Niveau),
                    ligne.VolumeValeur is { } valeur && ligne.VolumeUnite is { } unite
                        ? new Volume(valeur, UniteVolume.FromString(unite))
                        : null))));

    public static List<LigneStockDonnees> LignesVersDonnees(this Bar bar) =>
        [.. bar.Stock.Select(ligne => new LigneStockDonnees
        {
            ProprietaireId = bar.OwnerId,
            IngredientId = ligne.Key.Id,
            Niveau = ligne.Value.Niveau.ToString(),
            VolumeValeur = ligne.Value.Volume?.Value,
            VolumeUnite = ligne.Value.Volume?.Unit.ToString()
        })];

    public static Note VersDomaine(this NoteDonnees donnees) =>
        Note.Reconstituer(donnees.CocktailId, donnees.UtilisateurId, donnees.Valeur, donnees.NoteeLe);

    /// <summary>Recettes avec tout ce qu'il faut pour les reconstituer : lignes, ingrédients, alias, étapes.</summary>
    public static IQueryable<CocktailDonnees> AvecContenu(this IQueryable<CocktailDonnees> cocktails) =>
        cocktails
            .AsNoTracking()
            .Include(cocktail => cocktail.Composants).ThenInclude(composant => composant.Ingredient).ThenInclude(ingredient => ingredient!.Alias)
            .Include(cocktail => cocktail.Etapes)
            .AsSplitQuery();
}
