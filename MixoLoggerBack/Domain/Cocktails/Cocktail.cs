using Domain.Interfaces;
using Domain.Utilisateurs;

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

	/// <summary>
	/// L'utilisateur qui a créé la recette, seul autorisé à la modifier ou la supprimer.
	/// <c>null</c> pour les recettes d'origine de l'application, qui restent en lecture seule.
	/// </summary>
	public Guid? AuthorId { get; }

	public Cocktail(string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description = null, Guid? authorId = null)
		: this(Guid.NewGuid(), name, ingredients, etapes, description, authorId)
	{
	}

	private Cocktail(Guid id, string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description, Guid? authorId)
	{
		Id = id;

		if (authorId == Guid.Empty)
			throw new ArgumentException("L'auteur de la recette est invalide.", nameof(authorId));
		AuthorId = authorId;

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
	/// Recette relue depuis le stockage, avec son identifiant d'origine. Le contenu est validé
	/// comme à la création : une donnée corrompue en base ne produit pas de recette invalide.
	/// </summary>
	public static Cocktail Reconstituer(Guid id, string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description, Guid? authorId) =>
		new(id, name, ingredients, etapes, description, authorId);

	/// <summary>
	/// La même recette (même <see cref="Id"/>, même auteur) avec un nouveau contenu, validé comme à la création.
	/// </summary>
	public Cocktail Modifier(string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description = null) =>
		new(Id, name, ingredients, etapes, description, AuthorId);

	/// <summary>Vrai si cet utilisateur peut modifier ou supprimer la recette : il en est l'auteur.</summary>
	public bool EstModifiablePar(Guid utilisateurId) => AuthorId is { } auteur && auteur == utilisateurId;

	/// <exception cref="ActionNonAutoriseeException">L'utilisateur n'est pas l'auteur de la recette.</exception>
	public void VerifierModifiablePar(Guid utilisateurId)
	{
		if (!EstModifiablePar(utilisateurId))
			throw new ActionNonAutoriseeException(AuthorId is null
				? $"« {Name} » est une recette d'origine : elle ne peut pas être modifiée."
				: $"Seul l'auteur de « {Name} » peut la modifier ou la supprimer.");
	}
}
