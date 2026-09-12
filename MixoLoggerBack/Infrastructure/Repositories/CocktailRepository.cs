using System.Collections.Concurrent;
using Domain.Cocktails;
using Domain.Interfaces.Repositories;

namespace Infrastructure.Repositories;

public class CocktailRepository : ICocktailRepository
{
	// Simule un stockage en mémoire avec une liste initiale de cocktails
	private static readonly List<Cocktail> DEFAULT_COCKTAILS = [
		new Cocktail("Mojito",
		[
			new(IngredientReferentiel.Resolve("Rhum"), 50, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Menthe"), 10, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Citron vert"), 20, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Sucre"), 15, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Eau gazeuse"), 100, UniteVolume.Mililitre)
		],
			EtapeRecette.FromOrderedList([
				"Mixer la menthe et le sucre",
				"Ajouter le rhum et le jus de citron vert",
				"Ajouter de la glace pilée",
				"Compléter avec de l'eau gazeuse",
			]),
			"Un cocktail rafraîchissant à base de rhum et de menthe."),
		new Cocktail("Piña Colada",
		[
			new(IngredientReferentiel.Resolve("Rhum blanc"), 50, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Jus d'ananas"), 90, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Crème de coco"), 30, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Glace pilée"), 100, UniteVolume.Mililitre)
		],
			EtapeRecette.FromOrderedList([
				"Verser tous les ingrédients dans un shaker avec de la glace.",
				"Secouer vigoureusement.",
				"Servir dans un grand verre décoré d'une tranche d'ananas."
			]),
			"Un cocktail tropical doux et crémeux."
		),
		new Cocktail("Cosmopolitan",
		[
			new(IngredientReferentiel.Resolve("Vodka"), 40, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Triple sec"), 15, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Jus de cranberry"), 30, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Jus de citron vert"), 10, UniteVolume.Mililitre)
		],
			EtapeRecette.FromOrderedList([
				"Verser tous les ingrédients dans un shaker avec de la glace.",
				"Secouer et filtrer dans un verre à cocktail.",
				"Décorer avec un zeste de citron vert."
			]),
			"Un cocktail fruité et acidulé, très populaire."
		),
		new Cocktail("Tequila Sunrise",
		[
			new(IngredientReferentiel.Resolve("Tequila"), 40, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Jus d'orange"), 80, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Sirop de grenadine"), 10, UniteVolume.Mililitre)
		],
			EtapeRecette.FromOrderedList([
				"Verser la tequila et le jus d'orange dans un verre rempli de glace.",
				"Ajouter délicatement le sirop de grenadine.",
				"Ne pas mélanger pour obtenir l'effet 'sunrise'."
			]),
			"Un cocktail coloré rappelant un lever de soleil."
		),
		new Cocktail("Bloody Mary",
		[
			new(IngredientReferentiel.Resolve("Vodka"), 45, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Jus de tomate"), 90, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Jus de citron"), 15, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Sauce Worcestershire"), 2, UniteVolume.Mililitre),
			new(IngredientReferentiel.Resolve("Tabasco"), 1, UniteVolume.Mililitre)
		],
			EtapeRecette.FromOrderedList([
				"Verser tous les ingrédients dans un verre avec de la glace.",
				"Remuer doucement.",
				"Décorer avec une branche de céleri."
			]),
			"Un cocktail salé et épicé, idéal pour le brunch."
		),
	];
	private static readonly ConcurrentDictionary<Guid, Cocktail> _store = new(
		DEFAULT_COCKTAILS.ToDictionary(c => c.Id)
	);

	public Task<Cocktail?> GetByIdAsync(Guid id)
	{
		_store.TryGetValue(id, out var cocktail);
		return Task.FromResult(cocktail);
	}

	// Méthode d'ajout pour tests/démo
	public Task AddAsync(Cocktail cocktail)
	{
		_store[cocktail.Id] = cocktail;
		return Task.CompletedTask;
	}

	public Task<IEnumerable<Cocktail>> GetAllAsync()
	{
		return Task.FromResult(_store.Values.AsEnumerable());
	}

	public Task<IEnumerable<Cocktail>> GetByIngredientId(Guid ingredientId)
	{
		var result = _store.Values.Where(c => c.Ingredients.Any(i => i.Ingredient.Id == ingredientId));
		return Task.FromResult(result);
	}

	public Task UpdateAsync(Cocktail cocktail)
	{
		throw new NotImplementedException();
	}
}
