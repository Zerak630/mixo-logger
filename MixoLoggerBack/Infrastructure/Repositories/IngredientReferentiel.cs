using System.Collections.Concurrent;
using Domain.Cocktails;

namespace Infrastructure.Repositories;

/// <summary>
/// Référentiel plat des ingrédients : un nom canonique, des alias, et rien de plus
/// (cf. docs/STRATEGIE.md §7 — la hiérarchie catégorie/type/marque viendra plus tard).
/// </summary>
/// <remarks>
/// L'égalité d'<see cref="Ingredient"/> porte sur le nom normalisé. Le référentiel
/// sert donc à une chose : ramener les libellés équivalents (« Rhum », « rhum blanc »,
/// « white rum ») vers un seul et même ingrédient canonique, pour que le stock du bar
/// et les recettes parlent enfin de la même chose.
/// </remarks>
public static class IngredientReferentiel
{
	private static readonly ConcurrentDictionary<string, Ingredient> _parCle = new();

	static IngredientReferentiel()
	{
		// Alcools
		Register("Rhum blanc", "rhum", "white rum", "rhum agricole blanc");
		Register("Rhum ambré", "rhum brun", "dark rum", "rhum vieux");
		Register("Vodka");
		Register("Gin");
		Register("Tequila");
		Register("Whisky", "whiskey", "scotch");
		Register("Bourbon");
		Register("Cognac");
		Register("Triple sec", "cointreau", "curacao triple sec");
		Register("Curaçao bleu", "blue curacao");
		Register("Champagne");
		Register("Vin mousseux", "cremant", "prosecco");
		Register("Bitters", "angostura");

		// Jus et fruits
		Register("Jus d'orange", "orange");
		Register("Jus d'ananas", "ananas");
		Register("Jus de cranberry", "cranberry", "canneberge", "jus de canneberge");
		Register("Citron vert", "jus de citron vert", "lime", "jus de lime");
		Register("Citron", "jus de citron");

		// Sirops et sucrants
		Register("Sirop de sucre", "sucre", "sucre de canne", "sirop de canne");
		Register("Sirop de grenadine", "grenadine");
		Register("Sirop de menthe");

		// Sodas et eaux
		Register("Eau gazeuse", "eau petillante", "soda", "eau de seltz");
		Register("Tonic", "eau tonique", "schweppes");
		Register("Bière ginger", "ginger beer", "biere au gingembre");
		Register("Eau");

		// Laitiers et divers
		Register("Crème de coco");
		Register("Lait de coco");
		Register("Crème fraîche", "creme liquide");
		Register("Menthe", "menthe fraiche", "feuilles de menthe");
		Register("Glace pilée", "glacons", "glace");
	}

	/// <summary>Tous les ingrédients connus, triés par nom.</summary>
	public static IEnumerable<Ingredient> All =>
		_parCle.Values.Distinct().OrderBy(ingredient => ingredient.Name, StringComparer.CurrentCulture);

	/// <summary>
	/// Ramène un libellé libre vers l'ingrédient canonique. Un nom inconnu produit un
	/// nouvel ingrédient, enregistré au passage : le référentiel s'enrichit de ce que
	/// les utilisateurs saisissent réellement.
	/// </summary>
	public static Ingredient Resolve(string name)
	{
		return Find(name) ?? Register(name);
	}

	/// <summary>Comme <see cref="Resolve"/>, mais sans rien créer si le nom est inconnu.</summary>
	public static Ingredient? Find(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		return _parCle.GetValueOrDefault(IngredientName.Normalize(name));
	}

	public static Ingredient? FindById(Guid id) =>
		_parCle.Values.FirstOrDefault(ingredient => ingredient.Id == id);

	private static Ingredient Register(string name, params string[] aliases)
	{
		Ingredient ingredient = new(name, aliases);

		// Le nom canonique gagne toujours : un alias ne doit jamais écraser un ingrédient
		// existant, sans quoi l'index deviendrait dépendant de l'ordre d'enregistrement.
		_parCle[ingredient.NormalizedName] = ingredient;

		foreach (string alias in ingredient.Aliases)
		{
			_parCle.TryAdd(alias, ingredient);
		}

		return ingredient;
	}
}
