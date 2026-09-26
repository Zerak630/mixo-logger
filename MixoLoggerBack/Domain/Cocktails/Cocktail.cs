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

	/// <summary>
	/// Adresse d'une photo hébergée ailleurs (F9), <c>null</c> sans photo. Toujours une URL absolue
	/// en <c>https</c> : cf. <see cref="NormaliserPhotoUrl"/>.
	/// </summary>
	public string? PhotoUrl { get; }

	/// <summary>Longueur maximale de <see cref="PhotoUrl"/>, en caractères.</summary>
	public const int LongueurMaxPhotoUrl = 2000;

	public Cocktail(string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description = null, Guid? authorId = null, string? photoUrl = null)
		: this(Guid.NewGuid(), name, ingredients, etapes, description, authorId, photoUrl)
	{
	}

	private Cocktail(Guid id, string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description, Guid? authorId, string? photoUrl)
	{
		Id = id;
		PhotoUrl = NormaliserPhotoUrl(photoUrl);

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
	public static Cocktail Reconstituer(Guid id, string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description, Guid? authorId, string? photoUrl = null) =>
		new(id, name, ingredients, etapes, description, authorId, photoUrl);

	/// <summary>
	/// La même recette (même <see cref="Id"/>, même auteur) avec un nouveau contenu, validé comme à la création.
	/// Le contenu est remplacé en entier : sans <paramref name="photoUrl"/>, la photo est retirée.
	/// </summary>
	public Cocktail Modifier(string name, IEnumerable<CocktailIngredient> ingredients, IEnumerable<EtapeRecette> etapes, string? description = null, string? photoUrl = null) =>
		new(Id, name, ingredients, etapes, description, AuthorId, photoUrl);

	/// <summary>
	/// Une adresse vide devient « pas de photo ». Sinon, seule une URL absolue en <c>https</c>, sans
	/// identifiants, est acceptée : une image en <c>http</c> serait bloquée ou signalée par le
	/// navigateur une fois l'application servie en HTTPS, et <c>javascript:</c> ou <c>data:</c>
	/// n'ont rien à faire dans un attribut <c>src</c>.
	/// </summary>
	/// <returns>L'adresse sous sa forme canonique (<see cref="Uri.AbsoluteUri"/>), ou <c>null</c>.</returns>
	/// <exception cref="ArgumentException">Adresse trop longue, relative, ou dans un autre schéma que <c>https</c>.</exception>
	public static string? NormaliserPhotoUrl(string? photoUrl)
	{
		if (string.IsNullOrWhiteSpace(photoUrl))
			return null;

		string saisie = photoUrl.Trim();
		if (saisie.Length > LongueurMaxPhotoUrl)
			throw new ArgumentException($"L'adresse de la photo est trop longue ({LongueurMaxPhotoUrl} caractères au plus).", nameof(photoUrl));

		if (!Uri.TryCreate(saisie, UriKind.Absolute, out Uri? adresse) || adresse.Scheme != Uri.UriSchemeHttps)
			throw new ArgumentException("La photo doit être une adresse complète commençant par https://.", nameof(photoUrl));

		if (!string.IsNullOrEmpty(adresse.UserInfo))
			throw new ArgumentException("L'adresse de la photo ne doit pas contenir d'identifiant ni de mot de passe.", nameof(photoUrl));

		// La forme canonique peut s'allonger (caractères échappés) : on revérifie.
		string canonique = adresse.AbsoluteUri;
		if (canonique.Length > LongueurMaxPhotoUrl)
			throw new ArgumentException($"L'adresse de la photo est trop longue ({LongueurMaxPhotoUrl} caractères au plus).", nameof(photoUrl));

		return canonique;
	}

	/// <summary>Vrai si cet utilisateur peut modifier ou supprimer la recette : il en est l'auteur.</summary>
	public bool EstModifiablePar(Guid utilisateurId) => AuthorId is { } auteur && auteur == utilisateurId;

	/// <summary>
	/// Tout le monde note toutes les recettes, sauf la sienne : la note de l'auteur gonflerait la
	/// moyenne sans rien dire de ce qu'en pensent les autres. Les recettes d'origine n'ont pas d'auteur.
	/// </summary>
	public bool EstNotablePar(Guid utilisateurId) => AuthorId != utilisateurId;

	/// <exception cref="ActionNonAutoriseeException">L'utilisateur est l'auteur de la recette.</exception>
	public void VerifierNotablePar(Guid utilisateurId)
	{
		if (!EstNotablePar(utilisateurId))
			throw new ActionNonAutoriseeException($"« {Name} » est ta recette : tu ne peux pas la noter.");
	}

	/// <exception cref="ActionNonAutoriseeException">L'utilisateur n'est pas l'auteur de la recette.</exception>
	public void VerifierModifiablePar(Guid utilisateurId)
	{
		if (!EstModifiablePar(utilisateurId))
			throw new ActionNonAutoriseeException(AuthorId is null
				? $"« {Name} » est une recette d'origine : elle ne peut pas être modifiée."
				: $"Seul l'auteur de « {Name} » peut la modifier ou la supprimer.");
	}
}
