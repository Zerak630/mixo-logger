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
public class AuthController(IMediator mediator, LimiteurEchecsParCompte limiteur) : ControllerBase
{
    /// <summary>
    /// Ouvre une session : pose le cookie HttpOnly et renvoie l'utilisateur. 401 si
    /// l'identifiant ou le mot de passe est faux (sans dire lequel), 429 après trop d'essais,
    /// depuis la même adresse IP ou sur le même compte.
    /// </summary>
    [HttpPost("connexion")]
    [AllowAnonymous]
    [EnableRateLimiting(SecuriteExtensions.PolitiqueConnexion)]
    public async Task<ActionResult<UtilisateurDto>> Connexion([FromBody] ConnexionRequest demande, CancellationToken cancellationToken = default)
    {
        string identifiant = demande.Identifiant ?? string.Empty;

        // Avant toute vérification : un compte bloqué ne laisse plus rien tester, même le bon mot de passe.
        if (limiteur.EstBloque(identifiant, out TimeSpan attente))
            return this.TropDeTentatives(attente);

        IdentiteVerifiee? identite = await mediator.Send(
            new VerifierIdentifiantsQuery(identifiant, demande.MotDePasse ?? string.Empty),
            cancellationToken);

        if (identite is null)
        {
            limiteur.EnregistrerEchec(identifiant);
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Connexion refusée",
                detail: "Identifiant ou mot de passe incorrect.");
        }

        limiteur.Reinitialiser(identifiant);
        await HttpContext.OuvrirSessionAsync(identite.Utilisateur, identite.TamponSecurite);

        return identite.Utilisateur;
    }

    /// <summary>Ferme la session. Accessible sans être connecté : une session expirée doit pouvoir se fermer proprement.</summary>
    [HttpPost("deconnexion")]
    [AllowAnonymous]
    public async Task<IActionResult> Deconnexion()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>L'utilisateur de la session courante, lu en base (identifiant et nom à jour) ; 401 sans session.</summary>
    [HttpGet("moi")]
    public async Task<UtilisateurDto> Moi(CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetMonCompteQuery(), cancellationToken);
}

public record ConnexionRequest(string? Identifiant, string? MotDePasse);

internal static class ReponsesSecurite
{
    public static ObjectResult TropDeTentatives(this ControllerBase controleur, TimeSpan attente)
    {
        int minutes = Math.Max(1, (int)Math.Ceiling(attente.TotalMinutes));
        controleur.Response.Headers.RetryAfter = ((int)Math.Ceiling(attente.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);

        return controleur.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Trop de tentatives",
            detail: $"Trop de tentatives de connexion sur ce compte. Réessaie dans {minutes} minute{(minutes > 1 ? "s" : "")}.");
    }
}
