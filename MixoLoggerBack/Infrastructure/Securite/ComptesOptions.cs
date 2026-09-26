using Domain.Utilisateurs;

namespace Infrastructure.Securite;

/// <summary>
/// Section de configuration <c>Comptes</c> : les comptes que l'administrateur autorise.
/// </summary>
/// <remarks>
/// <para>
/// À renseigner hors du dépôt — user-secrets en développement, variables d'environnement sur le
/// serveur (cf. docs/MVP.md §10.2). Au démarrage, une entrée nouvelle crée le compte en base ;
/// ensuite, la base fait foi : le titulaire change lui-même son mot de passe et son identifiant.
/// </para>
/// <para>
/// Une entrée retirée désactive le compte (ses données restent) ; la remettre le réactive. Le mot de
/// passe n'est lu qu'à la création et avec <see cref="CompteConfigure.ReinitialiserMotDePasse"/> :
/// il peut ensuite être retiré de la configuration.
/// </para>
/// </remarks>
public class ComptesOptions
{
    public const string Section = "Comptes";

    /// <summary>Longueur minimale d'un mot de passe déclaré.</summary>
    public const int LongueurMinimaleMotDePasse = Utilisateur.LongueurMinimaleMotDePasse;

    public List<CompteConfigure> Liste { get; set; } = [];
}

public class CompteConfigure
{
    /// <summary>Identifiant à la création. Reste la clé de l'entrée, même si le titulaire en change ensuite.</summary>
    public string Identifiant { get; set; } = string.Empty;

    /// <summary>Lu à la création seulement : le titulaire le change ensuite lui-même.</summary>
    public string? NomAffiche { get; set; }

    /// <summary>Obligatoire à la création et avec <see cref="ReinitialiserMotDePasse"/> ; ignoré sinon.</summary>
    public string? MotDePasse { get; set; }

    /// <summary>
    /// Vrai pour remplacer le mot de passe en base par <see cref="MotDePasse"/> au prochain démarrage
    /// (mot de passe oublié). Ferme les sessions ouvertes. À remettre à faux ensuite, sinon chaque
    /// démarrage écrase le mot de passe choisi par le titulaire.
    /// </summary>
    public bool ReinitialiserMotDePasse { get; set; }
}
