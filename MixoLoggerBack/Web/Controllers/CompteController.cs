using System.Security.Claims;
using Application.Utilisateurs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Web.Securite;

namespace Web.Controllers;

/// <summary>Le compte de l'utilisateur connecté : identifiant, nom affiché, mot de passe.</summary>
[ApiController]
[Route("api/compte")]
public class CompteController(IMediator mediator, LimiteurEchecsParCompte limiteur) : ControllerBase
{
    [HttpGet]
    public async Task<UtilisateurDto> Get(CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetMonCompteQuery(), cancellationToken);

    /// <summary>
    /// Change l'identifiant de connexion et le nom affiché, sans rien perdre : bar, recettes et notes
    /// suivent le compte. 409 si l'identifiant est pris, 400 s'il est vide.
    /// </summary>
    [HttpPut]
    public async Task<UtilisateurDto> Modifier([FromBody] ModifierCompteRequest demande, CancellationToken cancellationToken = default)
    {
        UtilisateurDto compte = await mediator.Send(new ModifierMonCompteCommand(demande.Identifiant ?? string.Empty, demande.NomAffiche), cancellationToken);

        // La session porte l'identifiant et le nom : on la rouvre, avec le même tampon (les autres sessions restent valides).
        await HttpContext.OuvrirSessionAsync(compte, Guid.Parse(User.FindFirstValue(SecuriteExtensions.RevendicationTampon)!));
        return compte;
    }

    /// <summary>
    /// Change le mot de passe. 400 si l'actuel est faux (pas 401 : la session, elle, est valide) ou si
    /// le nouveau est refusé ; 429 après trop d'essais. Les autres sessions du compte sont fermées,
    /// celle-ci est conservée.
    /// </summary>
    [HttpPut("mot-de-passe")]
    [EnableRateLimiting(SecuriteExtensions.PolitiqueConnexion)]
    public async Task<IActionResult> ChangerMotDePasse([FromBody] ChangerMotDePasseRequest demande, CancellationToken cancellationToken = default)
    {
        // Même compteur que la connexion : une session volée ne permet pas de tester des mots de passe sans fin.
        string identifiant = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        if (limiteur.EstBloque(identifiant, out TimeSpan attente))
            return this.TropDeTentatives(attente);

        Guid? tampon = await mediator.Send(new ChangerMotDePasseCommand(demande.Actuel ?? string.Empty, demande.Nouveau ?? string.Empty), cancellationToken);

        if (tampon is null)
        {
            limiteur.EnregistrerEchec(identifiant);
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Mot de passe incorrect",
                detail: "Le mot de passe actuel est incorrect.");
        }

        limiteur.Reinitialiser(identifiant);
        await HttpContext.OuvrirSessionAsync(await mediator.Send(new GetMonCompteQuery(), cancellationToken), tampon.Value);
        return NoContent();
    }
}

public record ModifierCompteRequest(string? Identifiant, string? NomAffiche);

public record ChangerMotDePasseRequest(string? Actuel, string? Nouveau);
