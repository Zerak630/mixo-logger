using System.Security.Claims;
using Application.Utilisateurs;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Web.Securite;

namespace Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Ouvre une session : pose le cookie HttpOnly et renvoie l'utilisateur. 401 si
    /// l'identifiant ou le mot de passe est faux (sans dire lequel), 429 après trop d'essais.
    /// </summary>
    [HttpPost("connexion")]
    [AllowAnonymous]
    [EnableRateLimiting(SecuriteExtensions.PolitiqueConnexion)]
    public async Task<ActionResult<UtilisateurDto>> Connexion([FromBody] ConnexionRequest demande, CancellationToken cancellationToken = default)
    {
        UtilisateurDto? utilisateur = await mediator.Send(
            new VerifierIdentifiantsQuery(demande.Identifiant ?? string.Empty, demande.MotDePasse ?? string.Empty),
            cancellationToken);

        if (utilisateur is null)
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Connexion refusée",
                detail: "Identifiant ou mot de passe incorrect.");

        ClaimsIdentity identite = new(
            [
                new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
                new Claim(ClaimTypes.Name, utilisateur.Identifiant),
                new Claim(SecuriteExtensions.RevendicationNomAffiche, utilisateur.NomAffiche)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identite),
            // Session conservée à la fermeture du navigateur, jusqu'à 14 jours d'inactivité.
            new AuthenticationProperties { IsPersistent = true });

        return utilisateur;
    }

    /// <summary>Ferme la session. Accessible sans être connecté : une session expirée doit pouvoir se fermer proprement.</summary>
    [HttpPost("deconnexion")]
    [AllowAnonymous]
    public async Task<IActionResult> Deconnexion()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>L'utilisateur de la session courante ; 401 sans session.</summary>
    [HttpGet("moi")]
    public ActionResult<UtilisateurDto> Moi()
    {
        return new UtilisateurDto(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            User.FindFirstValue(ClaimTypes.Name)!,
            User.FindFirstValue(SecuriteExtensions.RevendicationNomAffiche)!);
    }
}

public record ConnexionRequest(string? Identifiant, string? MotDePasse);
