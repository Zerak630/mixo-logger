namespace Domain.Interfaces;

/// <summary>
/// Hachage des mots de passe. Abstrait ici pour que ni le domaine ni l'application ne
/// dépendent d'ASP.NET Core Identity, dont l'implémentation vit dans l'infrastructure.
/// </summary>
public interface IHacheurMotDePasse
{
    string Hacher(string motDePasse);

    /// <summary>Vrai si <paramref name="motDePasse"/> correspond à <paramref name="empreinte"/>.</summary>
    bool Verifier(string empreinte, string motDePasse);
}
