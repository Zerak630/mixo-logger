using Domain.Cocktails;
using Domain.Interfaces;

namespace Domain.MyBar;

public class Bar : IEntity
{
    private readonly Dictionary<Ingredient, LigneStock> _stock = [];

    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>L'utilisateur à qui appartient ce bar : un bar par personne (cf. docs/MVP.md §6.1).</summary>
    public Guid OwnerId { get; }

    /// <summary>
    /// Version de l'état lu, pour le contrôle de concurrence optimiste : le dépôt refuse
    /// une sauvegarde faite à partir d'une version périmée (cf. docs/MVP.md §7, B2).
    /// </summary>
    public int Version { get; set; }

    public IReadOnlyDictionary<Ingredient, LigneStock> Stock => _stock;

    public Bar(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Un bar appartient forcément à un utilisateur.", nameof(ownerId));

        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        OwnerId = ownerId;
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
        Bar copie = new(OwnerId) { Id = Id, CreatedAt = CreatedAt, Version = Version };

        foreach ((Ingredient ingredient, LigneStock ligne) in _stock)
        {
            copie._stock[ingredient] = ligne;
        }

        return copie;
    }

    /// <summary>Bar relu depuis le stockage : identité, version et lignes telles qu'enregistrées.</summary>
    public static Bar Reconstituer(Guid id, Guid ownerId, DateTime createdAt, int version, IEnumerable<KeyValuePair<Ingredient, LigneStock>> stock)
    {
        ArgumentNullException.ThrowIfNull(stock, nameof(stock));

        Bar bar = new(ownerId) { Id = id, CreatedAt = createdAt, Version = version };

        foreach ((Ingredient ingredient, LigneStock ligne) in stock)
        {
            ArgumentNullException.ThrowIfNull(ingredient, nameof(stock));
            bar._stock[ingredient] = ligne ?? throw new ArgumentNullException(nameof(stock));
        }

        return bar;
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
    public bool CanMakeAll(IEnumerable<CommandeCocktail> commande) => Manques(commande).Count == 0;

    /// <summary>Ce qui manque pour préparer <paramref name="quantite"/> fois ce cocktail (vide s'il est réalisable).</summary>
    public IReadOnlyList<Manque> Manques(Cocktail cocktail, int quantite = 1) =>
        Manques([new CommandeCocktail(cocktail, quantite)]);

    /// <summary>
    /// Ce qui manque pour préparer la commande entière, dans l'ordre des recettes.
    /// C'est l'unique calcul de faisabilité : <see cref="CanMakeAll"/> et
    /// <see cref="MakeCocktails"/> s'appuient dessus, pour que « réalisable » à l'écran
    /// et « préparable » à la commande ne puissent jamais diverger.
    /// </summary>
    public IReadOnlyList<Manque> Manques(IEnumerable<CommandeCocktail> commande)
    {
        ArgumentNullException.ThrowIfNull(commande, nameof(commande));

        List<Manque> manques = [];

        foreach ((Ingredient ingredient, Volume? requis) in Cumuler(commande))
        {
            if (!_stock.TryGetValue(ingredient, out LigneStock? ligne))
                manques.Add(new Manque(ingredient, RaisonManque.Absent, requis));
            else if (requis is not null && !ligne.Couvre(requis))
                manques.Add(new Manque(ingredient, RaisonManque.Insuffisant, requis));
        }

        return manques;
    }

    /// <summary>
    /// Prépare la commande entière, ou rien du tout : la faisabilité est vérifiée sur
    /// la totalité des lignes avant la moindre consommation.
    /// </summary>
    public void MakeCocktails(IEnumerable<CommandeCocktail> commande)
    {
        ArgumentNullException.ThrowIfNull(commande, nameof(commande));

        List<CommandeCocktail> lignes = [.. commande];
        IReadOnlyList<Manque> manques = Manques(lignes);

        if (manques.Count > 0)
        {
            string detail = string.Join(", ", manques.Select(manque => manque.Raison == RaisonManque.Absent
                ? $"« {manque.Ingredient.Name} » absent"
                : $"« {manque.Ingredient.Name} » en quantité insuffisante"));

            throw new InvalidOperationException($"Commande impossible : {detail}.");
        }

        foreach ((Ingredient ingredient, Volume? requis) in Cumuler(lignes))
        {
            // Un décompte (« 6 feuilles ») ne se retire pas : le bar ne suit que des volumes.
            if (requis is not null)
                _stock[ingredient] = _stock[ingredient].Retirer(requis);
        }
    }

    /// <summary>
    /// Besoins par ingrédient sur toute la commande : le volume total réclamé, ou
    /// <c>null</c> quand l'ingrédient n'est demandé qu'en décompte — sa seule présence
    /// suffit alors (cf. <see cref="Dose"/>).
    /// </summary>
    private static Dictionary<Ingredient, Volume?> Cumuler(IEnumerable<CommandeCocktail> commande)
    {
        Dictionary<Ingredient, Volume?> besoins = [];

        foreach (CommandeCocktail ligne in commande)
        {
            ArgumentNullException.ThrowIfNull(ligne, nameof(commande));

            foreach (CocktailIngredient composant in ligne.Cocktail.Ingredients)
            {
                Volume? requis = composant.Volume is null ? null : composant.Volume * ligne.Quantite;

                if (!besoins.TryGetValue(composant.Ingredient, out Volume? deja))
                    besoins[composant.Ingredient] = requis;
                else if (requis is not null)
                    besoins[composant.Ingredient] = deja is null ? requis : deja + requis;
            }
        }

        return besoins;
    }
}
