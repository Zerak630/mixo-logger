namespace Infrastructure.Persistance;

// Modèles de stockage : de simples conteneurs de colonnes, traduits vers et depuis le domaine
// par les dépôts. Le domaine reste ainsi immuable et sans dépendance à EF Core (constructeurs
// validants, objets valeur, dictionnaire de stock indexé par ingrédient…).

public class IngredientDonnees
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }

    /// <summary>Clé d'identité du domaine (<c>IngredientName.Normalize</c>), unique.</summary>
    public required string NomNormalise { get; set; }

    public DateTime CreeLe { get; set; }
    public List<AliasDonnees> Alias { get; set; } = [];
}

/// <summary>Un alias normalisé ne désigne qu'un seul ingrédient : c'est la clé primaire.</summary>
public class AliasDonnees
{
    public required string Alias { get; set; }
    public Guid IngredientId { get; set; }
}

public class CocktailDonnees
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }

    /// <summary>Unique : deux recettes ne peuvent pas porter le même nom, accents et casse ignorés.</summary>
    public required string NomNormalise { get; set; }

    public string? Description { get; set; }
    public Guid? AuteurId { get; set; }
    public List<ComposantDonnees> Composants { get; set; } = [];
    public List<EtapeDonnees> Etapes { get; set; } = [];
}

public class ComposantDonnees
{
    public Guid CocktailId { get; set; }
    public Guid IngredientId { get; set; }
    public IngredientDonnees? Ingredient { get; set; }

    /// <summary>Rang dans la recette : l'ordre de saisie est conservé.</summary>
    public int Position { get; set; }

    public double Valeur { get; set; }
    public required string Unite { get; set; }
}

public class EtapeDonnees
{
    public Guid CocktailId { get; set; }
    public int Ordre { get; set; }
    public required string Description { get; set; }
}

/// <summary>Un bar par utilisateur : le propriétaire est la clé.</summary>
public class BarDonnees
{
    public Guid ProprietaireId { get; set; }
    public Guid Id { get; set; }
    public DateTime CreeLe { get; set; }

    /// <summary>Contrôle de concurrence optimiste : incrémentée à chaque sauvegarde.</summary>
    public int Version { get; set; }

    public List<LigneStockDonnees> Lignes { get; set; } = [];
}

public class LigneStockDonnees
{
    public Guid ProprietaireId { get; set; }
    public Guid IngredientId { get; set; }
    public IngredientDonnees? Ingredient { get; set; }
    public required string Niveau { get; set; }

    /// <summary>Renseignés ensemble en suivi précis, tous deux vides en possession simple.</summary>
    public double? VolumeValeur { get; set; }

    public string? VolumeUnite { get; set; }
}

public class NoteDonnees
{
    public Guid CocktailId { get; set; }
    public Guid UtilisateurId { get; set; }
    public int Valeur { get; set; }
    public DateTime NoteeLe { get; set; }
}
