using Domain.Utilisateurs;

namespace Domain.Interfaces.Repositories;

public interface IUtilisateurRepository
{
    /// <summary>Recherche par identifiant, sans tenir compte de la casse ni des accents.</summary>
    Task<Utilisateur?> GetByIdentifiantAsync(string identifiant);

    Task<Utilisateur?> GetByIdAsync(Guid id);
}
