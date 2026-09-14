namespace Infrastructure.Securite;

/// <summary>
/// Section de configuration <c>Comptes</c> : la liste des comptes autorisés.
/// </summary>
/// <remarks>
/// À renseigner hors du dépôt — user-secrets en développement, variables d'environnement
/// sur le serveur (cf. docs/MVP.md §10.1). Les mots de passe sont hachés au démarrage et
/// ne sont plus conservés en clair ensuite.
/// </remarks>
public class ComptesOptions
{
    public const string Section = "Comptes";

    /// <summary>Longueur minimale d'un mot de passe déclaré.</summary>
    public const int LongueurMinimaleMotDePasse = 12;

    public List<CompteConfigure> Liste { get; set; } = [];
}

public class CompteConfigure
{
    public string Identifiant { get; set; } = string.Empty;
    public string? NomAffiche { get; set; }
    public string MotDePasse { get; set; } = string.Empty;
}
