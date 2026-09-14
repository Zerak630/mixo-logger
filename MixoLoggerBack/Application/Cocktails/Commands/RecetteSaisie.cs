using Domain.Cocktails;
using Domain.Interfaces.Repositories;

namespace Application.Cocktails.Commands;

/// <summary>Contenu d'une recette tel que saisi : commun à la création et à l'édition.</summary>
public class RecetteSaisie
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public List<DoseSaisie> Ingredients { get; init; } = [];

    /// <summary>Étapes dans l'ordre : leur position fait leur numéro.</summary>
    public List<string> Etapes { get; init; } = [];
}

/// <param name="Name">Nom libre : résolu par le référentiel (alias compris), créé s'il est inconnu.</param>
public record DoseSaisie(string Name, double Valeur, string Unite);

/// <summary>Traduction d'une <see cref="RecetteSaisie"/> en objets du domaine.</summary>
internal static class RecetteSaisieExtensions
{
    /// <summary>
    /// Construit la recette via <paramref name="fabrique"/>, en ne créant les ingrédients
    /// inconnus dans le référentiel qu'une fois la recette entière jugée valide.
    /// </summary>
    /// <remarks>
    /// Une première construction se fait avec des ingrédients provisoires, non enregistrés :
    /// c'est elle qui valide (nom, étapes, doublons…). Sans cela, une recette refusée pour une
    /// étape vide aurait déjà ajouté ses ingrédients inconnus au référentiel — et donc à
    /// l'autocomplétion de tout le monde. L'égalité d'<see cref="Ingredient"/> portant sur le
    /// nom normalisé, un ingrédient provisoire et son équivalent enregistré sont interchangeables.
    /// </remarks>
    public static async Task<Cocktail> ConstruireAsync(
        this RecetteSaisie saisie,
        IIngredientRepository ingredientRepository,
        Func<IReadOnlyList<CocktailIngredient>, Cocktail> fabrique)
    {
        List<(DoseSaisie Saisie, Dose Dose, Ingredient? Connu)> lignes = [];

        foreach (DoseSaisie dose in saisie.Ingredients ?? [])
        {
            if (dose is null || string.IsNullOrWhiteSpace(dose.Name))
                throw new ArgumentException("Chaque ingrédient de la recette doit avoir un nom.", nameof(saisie));

            lignes.Add((dose, new Dose(dose.Valeur, dose.Unite), await ingredientRepository.GetByNameAsync(dose.Name.Trim())));
        }

        // 1. Validation complète, sans rien enregistrer.
        fabrique([.. lignes.Select(ligne => new CocktailIngredient(ligne.Connu ?? new Ingredient(ligne.Saisie.Name.Trim()), ligne.Dose))]);

        // 2. La recette est valide : on peut enrichir le référentiel.
        List<CocktailIngredient> composants = [];
        foreach ((DoseSaisie dose, Dose valeur, Ingredient? connu) in lignes)
        {
            Ingredient ingredient = connu ?? await ingredientRepository.GetOrCreateAsync(dose.Name.Trim());
            composants.Add(new CocktailIngredient(ingredient, valeur));
        }

        return fabrique(composants);
    }

    public static IEnumerable<EtapeRecette> Etapes(this RecetteSaisie saisie) =>
        EtapeRecette.FromOrderedList(saisie.Etapes ?? []);

    /// <summary>
    /// Refuse un nom déjà porté par une autre recette, sans tenir compte des accents ni de
    /// la casse. <paramref name="idModifie"/> exclut la recette en cours d'édition.
    /// </summary>
    /// <remarks>
    /// Vérification puis écriture ne sont pas atomiques : deux créations simultanées du même
    /// nom peuvent passer. Sans conséquence ici (deux recettes homonymes), à reprendre avec une
    /// contrainte d'unicité en base.
    /// </remarks>
    public static async Task VerifierNomDisponibleAsync(
        this RecetteSaisie saisie,
        ICocktailRepository cocktailRepository,
        Guid? idModifie = null)
    {
        if (string.IsNullOrWhiteSpace(saisie.Name))
            return; // Le domaine lèvera l'erreur de nom obligatoire, avec son propre message.

        string nom = IngredientName.Normalize(saisie.Name);

        bool pris = (await cocktailRepository.GetAllAsync())
            .Any(cocktail => cocktail.Id != idModifie && IngredientName.Normalize(cocktail.Name) == nom);

        if (pris)
            throw new InvalidOperationException($"Une recette s'appelle déjà « {saisie.Name.Trim()} ».");
    }
}
