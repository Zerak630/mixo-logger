namespace Application.Utilisateurs;

/// <summary>
/// L'utilisateur à l'origine de la requête en cours. Fourni par la couche web à partir de la
/// session : un handler ne lit jamais l'identité dans le corps de la requête, qu'un client
/// pourrait falsifier.
/// </summary>
public interface IUtilisateurCourant
{
    /// <exception cref="UnauthorizedAccessException">Aucune session : ne se produit pas derrière la politique d'autorisation par défaut.</exception>
    Guid Id { get; }
}
