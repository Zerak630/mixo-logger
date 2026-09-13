using Domain.Cocktails;

namespace Domain.MyBar;

/// <summary>Pourquoi un ingrédient empêche de préparer une commande.</summary>
public enum RaisonManque
{
    /// <summary>L'ingrédient n'est pas dans le bar.</summary>
    Absent,

    /// <summary>L'ingrédient est suivi en volume, et ce volume ne couvre pas le besoin.</summary>
    Insuffisant
}

/// <summary>Un ingrédient qui manque pour préparer une commande, et pourquoi.</summary>
public record Manque(Ingredient Ingredient, RaisonManque Raison, Volume Requis);
