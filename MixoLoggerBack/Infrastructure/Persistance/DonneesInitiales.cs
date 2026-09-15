using Domain.Cocktails;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance;

/// <summary>
/// Référentiel d'ingrédients et recettes d'origine, insérés <b>une seule fois</b>, dans une base
/// vide. Ensuite la base fait foi : modifier cette liste ne touche pas une base existante.
/// </summary>
public static class DonneesInitiales
{
    /// <summary>Noms canoniques et alias (cf. docs/STRATEGIE.md §7 — hiérarchie catégorie/type/marque plus tard).</summary>
    private static readonly (string Nom, string[] Alias)[] Referentiel =
    [
        // Alcools
        ("Rhum blanc", ["rhum", "white rum", "rhum agricole blanc"]),
        ("Rhum ambré", ["rhum brun", "dark rum", "rhum vieux"]),
        ("Vodka", []),
        ("Gin", []),
        ("Tequila", []),
        ("Whisky", ["whiskey", "scotch"]),
        ("Bourbon", []),
        ("Cognac", []),
        ("Triple sec", ["cointreau", "curacao triple sec"]),
        ("Curaçao bleu", ["blue curacao"]),
        ("Champagne", []),
        ("Vin mousseux", ["cremant", "prosecco"]),
        ("Bitters", ["angostura"]),

        // Jus et fruits
        ("Jus d'orange", ["orange"]),
        ("Jus d'ananas", ["ananas"]),
        ("Jus de cranberry", ["cranberry", "canneberge", "jus de canneberge"]),
        ("Citron vert", ["jus de citron vert", "lime", "jus de lime"]),
        ("Citron", ["jus de citron"]),

        // Sirops et sucrants
        ("Sirop de sucre", ["sucre", "sucre de canne", "sirop de canne"]),
        ("Sirop de grenadine", ["grenadine"]),
        ("Sirop de menthe", []),

        // Sodas et eaux
        ("Eau gazeuse", ["eau petillante", "soda", "eau de seltz"]),
        ("Tonic", ["eau tonique", "schweppes"]),
        ("Bière ginger", ["ginger beer", "biere au gingembre"]),
        ("Eau", []),

        // Laitiers et divers
        ("Crème de coco", []),
        ("Lait de coco", []),
        ("Crème fraîche", ["creme liquide"]),
        ("Menthe", ["menthe fraiche", "feuilles de menthe"]),
        ("Glace pilée", ["glacons", "glace"]),
    ];

    /// <summary>Insère le jeu initial si la base ne contient encore ni ingrédient ni recette.</summary>
    public static async Task InsererSiVideAsync(DbContext contexte, CancellationToken annulation = default)
    {
        var db = (MixoLoggerDbContext)contexte;

        if (await db.Ingredients.AnyAsync(annulation) || await db.Cocktails.AnyAsync(annulation))
            return;

        ReferentielEnConstruction referentiel = new();
        foreach ((string nom, string[] alias) in Referentiel)
            referentiel.Enregistrer(nom, alias);

        List<Cocktail> recettes = Recettes(referentiel);

        db.Ingredients.AddRange(referentiel.Tous.Select(ingredient => ingredient.VersDonnees()));
        db.Cocktails.AddRange(recettes.Select(recette => recette.VersDonnees()));
        await db.SaveChangesAsync(annulation);
        db.ChangeTracker.Clear();
    }

    /// <summary>Version synchrone, exigée par EF Core à côté de la version asynchrone.</summary>
    public static void InsererSiVide(DbContext contexte) =>
        InsererSiVideAsync(contexte).GetAwaiter().GetResult();

    /// <summary>Recettes d'origine : sans auteur, donc en lecture seule pour tous.</summary>
    private static List<Cocktail> Recettes(ReferentielEnConstruction r) =>
    [
        new Cocktail("Mojito",
            [
                new(r.Resoudre("Rhum"), 50, UniteVolume.Mililitre),
                new(r.Resoudre("Menthe"), 6, Dose.Feuille),
                new(r.Resoudre("Citron vert"), 20, UniteVolume.Mililitre),
                new(r.Resoudre("Sucre"), 15, UniteVolume.Mililitre),
                new(r.Resoudre("Eau gazeuse"), 100, UniteVolume.Mililitre)
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
                new(r.Resoudre("Rhum blanc"), 50, UniteVolume.Mililitre),
                new(r.Resoudre("Jus d'ananas"), 90, UniteVolume.Mililitre),
                new(r.Resoudre("Crème de coco"), 30, UniteVolume.Mililitre),
                new(r.Resoudre("Glace pilée"), 100, UniteVolume.Mililitre)
            ],
            EtapeRecette.FromOrderedList([
                "Verser tous les ingrédients dans un shaker avec de la glace.",
                "Secouer vigoureusement.",
                "Servir dans un grand verre décoré d'une tranche d'ananas."
            ]),
            "Un cocktail tropical doux et crémeux."),
        new Cocktail("Cosmopolitan",
            [
                new(r.Resoudre("Vodka"), 40, UniteVolume.Mililitre),
                new(r.Resoudre("Triple sec"), 15, UniteVolume.Mililitre),
                new(r.Resoudre("Jus de cranberry"), 30, UniteVolume.Mililitre),
                new(r.Resoudre("Jus de citron vert"), 10, UniteVolume.Mililitre)
            ],
            EtapeRecette.FromOrderedList([
                "Verser tous les ingrédients dans un shaker avec de la glace.",
                "Secouer et filtrer dans un verre à cocktail.",
                "Décorer avec un zeste de citron vert."
            ]),
            "Un cocktail fruité et acidulé, très populaire."),
        new Cocktail("Tequila Sunrise",
            [
                new(r.Resoudre("Tequila"), 40, UniteVolume.Mililitre),
                new(r.Resoudre("Jus d'orange"), 80, UniteVolume.Mililitre),
                new(r.Resoudre("Sirop de grenadine"), 10, UniteVolume.Mililitre)
            ],
            EtapeRecette.FromOrderedList([
                "Verser la tequila et le jus d'orange dans un verre rempli de glace.",
                "Ajouter délicatement le sirop de grenadine.",
                "Ne pas mélanger pour obtenir l'effet 'sunrise'."
            ]),
            "Un cocktail coloré rappelant un lever de soleil."),
        new Cocktail("Bloody Mary",
            [
                new(r.Resoudre("Vodka"), 45, UniteVolume.Mililitre),
                new(r.Resoudre("Jus de tomate"), 90, UniteVolume.Mililitre),
                new(r.Resoudre("Jus de citron"), 15, UniteVolume.Mililitre),
                new(r.Resoudre("Sauce Worcestershire"), 3, Dose.Trait),
                new(r.Resoudre("Tabasco"), 2, Dose.Trait)
            ],
            EtapeRecette.FromOrderedList([
                "Verser tous les ingrédients dans un verre avec de la glace.",
                "Remuer doucement.",
                "Décorer avec une branche de céleri."
            ]),
            "Un cocktail salé et épicé, idéal pour le brunch."),
    ];

    /// <summary>Résolution nom / alias en mémoire, le temps de construire le jeu initial.</summary>
    private sealed class ReferentielEnConstruction
    {
        private readonly Dictionary<string, Ingredient> _parCle = [];
        private readonly List<Ingredient> _tous = [];

        public IReadOnlyList<Ingredient> Tous => _tous;

        public void Enregistrer(string nom, string[] alias)
        {
            // Un alias déjà pris (nom ou alias d'un autre ingrédient) est ignoré : la table des
            // alias l'impose, et le premier enregistré doit rester le seul à le porter.
            Ingredient ingredient = new(nom, [.. alias.Where(a => !_parCle.ContainsKey(IngredientName.Normalize(a)))]);

            _parCle[ingredient.NormalizedName] = ingredient;
            foreach (string cle in ingredient.Aliases)
                _parCle.TryAdd(cle, ingredient);

            _tous.Add(ingredient);
        }

        /// <summary>Un nom inconnu devient un ingrédient sans alias.</summary>
        public Ingredient Resoudre(string nom)
        {
            if (_parCle.TryGetValue(IngredientName.Normalize(nom), out Ingredient? connu))
                return connu;

            Enregistrer(nom, []);
            return _parCle[IngredientName.Normalize(nom)];
        }
    }
}
