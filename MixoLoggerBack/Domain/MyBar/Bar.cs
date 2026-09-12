using Domain.Cocktails;
using Domain.Interfaces;

namespace Domain.MyBar;

public class Bar : IEntity
{
    private readonly Dictionary<Ingredient, LigneStock> _stock = [];

    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Version de l'état lu, pour le contrôle de concurrence optimiste : le dépôt refuse
    /// une sauvegarde faite à partir d'une version périmée (cf. docs/MVP.md §7, B2).
    /// </summary>
    public int Version { get; set; }

    public IReadOnlyDictionary<Ingredient, LigneStock> Stock => _stock;

    public Bar()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Copie indépendante du bar. Une copie superficielle du dictionnaire suffit :
    /// <see cref="Ingredient"/> et <see cref="LigneStock"/> sont immuables.
    /// </summary>
    /// <remarks>
    /// Sert à isoler les requêtes les unes des autres : chacune travaille sur sa
    /// propre instance, puis publie son résultat par <c>IBarRepository.SaveAsync</c>
    /// (cf. docs/MVP.md §7, B2). Sans cela, deux requêtes concurrentes muteraient le
    /// même <see cref="Dictionary{TKey, TValue}"/> et pourraient le corrompre.
    /// </remarks>
    public Bar Snapshot()
    {
        Bar copie = new() { Id = Id, CreatedAt = CreatedAt, Version = Version };

        foreach ((Ingredient ingredient, LigneStock ligne) in _stock)
        {
            copie._stock[ingredient] = ligne;
        }

        return copie;
    }

    /// <summary>
    /// Déclare la possession d'un ingrédient, sans suivi de volume. C'est le geste
    /// par défaut : « j'ai du rhum blanc ».
    /// </summary>
    public void AddIngredient(Ingredient ingredient, NiveauStock? niveau = null)
    {
        ArgumentNullException.ThrowIfNull(ingredient, nameof(ingredient));

        _stock[ingredient] = new LigneStock(niveau ?? NiveauStock.Pleine);
    }

    /// <summary>
    /// Déclare la possession d'un ingrédient avec suivi précis du volume. Un ajout
    /// répété cumule les volumes plutôt que de dupliquer la ligne.
    /// </summary>
    public void AddIngredient(Ingredient ingredient, Volume addedVolume)
    {
        ArgumentNullException.ThrowIfNull(ingredient, nameof(ingredient));
        ArgumentNullException.ThrowIfNull(addedVolume, nameof(addedVolume));

        _stock[ingredient] = _stock.TryGetValue(ingredient, out LigneStock? ligne)
            ? ligne.Ajouter(addedVolume)
            : new LigneStock(NiveauStock.Pleine, addedVolume);
    }

    /// <summary>Corrige le niveau approximatif d'une ligne existante.</summary>
    public void SetNiveau(Ingredient ingredient, NiveauStock niveau)
    {
        ArgumentNullException.ThrowIfNull(ingredient, nameof(ingredient));
        ArgumentNullException.ThrowIfNull(niveau, nameof(niveau));

        if (!_stock.TryGetValue(ingredient, out LigneStock? ligne))
            throw new KeyNotFoundException($"L'ingrédient « {ingredient.Name} » n'est pas dans le bar.");

        _stock[ingredient] = new LigneStock(niveau, ligne.Volume);
    }

    /// <summary>Retire complètement un ingrédient du bar. Idempotent.</summary>
    public bool RemoveIngredient(Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient, nameof(ingredient));

        return _stock.Remove(ingredient);
    }

    public bool Has(Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient, nameof(ingredient));

        return _stock.ContainsKey(ingredient);
    }

    public bool CanMake(Cocktail cocktail, int quantite = 1) =>
        CanMakeAll([new CommandeCocktail(cocktail, quantite)]);

    public void MakeCocktail(Cocktail cocktail, int quantite = 1) =>
        MakeCocktails([new CommandeCocktail(cocktail, quantite)]);

    /// <summary>
    /// Vrai si la commande entière est réalisable. Les besoins sont cumulés sur
    /// l'ensemble des lignes avant comparaison : deux cocktails réclamant chacun
    /// 50 mL de rhum ne passent pas avec 80 mL en stock (cf. docs/MVP.md §7, B11/B12).
    /// </summary>
    public bool CanMakeAll(IEnumerable<CommandeCocktail> commande)
    {
        ArgumentNullException.ThrowIfNull(commande, nameof(commande));

        foreach ((Ingredient ingredient, Volume requis) in Cumuler(commande))
        {
            if (!_stock.TryGetValue(ingredient, out LigneStock? ligne) || !ligne.Couvre(requis))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Prépare la commande entière, ou rien du tout : la faisabilité est vérifiée sur
    /// la totalité des lignes avant la moindre consommation.
    /// </summary>
    public void MakeCocktails(IEnumerable<CommandeCocktail> commande)
    {
        ArgumentNullException.ThrowIfNull(commande, nameof(commande));

        Dictionary<Ingredient, Volume> besoins = Cumuler(commande);

        foreach ((Ingredient ingredient, Volume requis) in besoins)
        {
            if (!_stock.TryGetValue(ingredient, out LigneStock? ligne) || !ligne.Couvre(requis))
                throw new InvalidOperationException(
                    $"Commande impossible : « {ingredient.Name} » manque ou est en quantité insuffisante.");
        }

        foreach ((Ingredient ingredient, Volume requis) in besoins)
        {
            _stock[ingredient] = _stock[ingredient].Retirer(requis);
        }
    }

    /// <summary>Somme les volumes réclamés par ingrédient sur toute la commande.</summary>
    private static Dictionary<Ingredient, Volume> Cumuler(IEnumerable<CommandeCocktail> commande)
    {
        Dictionary<Ingredient, Volume> besoins = [];

        foreach (CommandeCocktail ligne in commande)
        {
            ArgumentNullException.ThrowIfNull(ligne, nameof(commande));

            foreach (CocktailIngredient composant in ligne.Cocktail.Ingredients)
            {
                Volume requis = composant.Volume * ligne.Quantite;

                besoins[composant.Ingredient] = besoins.TryGetValue(composant.Ingredient, out Volume? deja)
                    ? deja + requis
                    : requis;
            }
        }

        return besoins;
    }
}
