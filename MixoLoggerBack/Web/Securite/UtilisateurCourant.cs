using System.Security.Claims;
using Application.Utilisateurs;

namespace Web.Securite;

/// <summary>Lit l'identifiant de l'utilisateur dans le cookie de session de la requête en cours.</summary>
public class UtilisateurCourant(IHttpContextAccessor accesseur) : IUtilisateurCourant
{
    public Guid Id =>
        Guid.TryParse(accesseur.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id)
            ? id
            : throw new UnauthorizedAccessException("Aucun utilisateur connecté.");
}
