using Domain.Interfaces;

namespace Domain.Cocktails;

/// <summary>
/// Une recette. Immuable : une modification produit une nouvelle instance de même
/// <see cref="Id"/> (<see cref="Modifier"/>), que le dépôt substitue d'un bloc. Une lecture
/// concurrente ne voit ainsi jamais une recette à moitié modifiée.
/// </summary>
public class Cocktail : IAggregate
{
	public Guid Id { get; }
	public string Name { get; }
	public string? Description { get; }
	public IReadOnlyCollection<CocktailIngredient> Ingredients { get; }
	public IReadOnlyCollection<EtapeRecette> EtapeRecettes { get; }

	public Cocktail(string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description = null)
		: this(Guid.NewGuid(), name, ingredients, etapes, description)
	{
	}

	private Cocktail(Guid id, string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description)
	{
		Id = id;

		ArgumentNullException.ThrowIfNull(name, nameof(name));
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Le nom du cocktail est obligatoire.", nameof(name));
		Name = name.Trim();

		Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

		// Matérialisées une seule fois : EtapeRecette.FromOrderedList est paresseux, et
		// l'énumérer deux fois recréerait les étapes avec de nouveaux identifiants.
		List<CocktailIngredient> listeIngredients = [.. ingredients ?? []];
		if (listeIngredients.Count == 0)
			throw new ArgumentException("Un cocktail doit avoir au moins un ingrédient.", nameof(ingredients));

		// Deux lignes pour le même ingrédient rendraient la recette ambiguë (« 5 cL de rhum…
		// et 2 cL de rhum ») et fausseraient la lecture du stock. On demande de les fusionner.
		string? doublon = listeIngredients
			.GroupBy(composant => composant.Ingredient)
			.Where(groupe => groupe.Count() > 1)
			.Select(groupe => groupe.Key.Name)
			.FirstOrDefault();
		if (doublon is not null)
			throw new ArgumentException($"L'ingrédient « {doublon} » apparaît plusieurs fois dans la recette.", nameof(ingredients));

		Ingredients = listeIngredients.AsReadOnly();

		List<EtapeRecette> listeEtapes = [.. (etapes ?? []).OrderBy(etape => etape.Ordre)];
		if (listeEtapes.Count == 0)
			throw new ArgumentException("Un cocktail doit avoir au moins une étape.", nameof(etapes));
		EtapeRecettes = listeEtapes.AsReadOnly();
	}

	/// <summary>
	/// La même recette (même <see cref="Id"/>) avec un nouveau contenu, validé comme à la création.
	/// </summary>
	public Cocktail Modifier(string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description = null) =>
		new(Id, name, ingredients, etapes, description);
}
