using Domain.Utilisateurs;

namespace Domain.Interfaces.Repositories;

/// <summary>Comptes en base. Les lectures ne renvoient que les comptes actifs : un compte désactivé n'existe plus pour l'application.</summary>
public interface IUtilisateurRepository
{
    /// <summary>Recherche par identifiant, sans tenir compte de la casse ni des accents.</summary>
    Task<Utilisateur?> GetByIdentifiantAsync(string identifiant);

    Task<Utilisateur?> GetByIdAsync(Guid id);

    /// <summary>Enregistre identifiant, nom affiché, empreinte et tampon.</summary>
    /// <exception cref="InvalidOperationException">L'identifiant est déjà pris par un autre compte (409).</exception>
    /// <exception cref="KeyNotFoundException">Le compte n'existe plus.</exception>
    Task MettreAJourAsync(Utilisateur utilisateur);
}
