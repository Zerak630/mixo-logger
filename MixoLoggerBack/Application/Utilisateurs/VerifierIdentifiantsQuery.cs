using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Utilisateurs;
using MediatR;

namespace Application.Utilisateurs;

/// <summary>
/// Vérifie un couple identifiant / mot de passe. Renvoie l'utilisateur, ou <c>null</c> si
/// l'un ou l'autre est faux — sans jamais dire lequel.
/// </summary>
/// <remarks>
/// L'ouverture de session elle-même (cookie) est une affaire du web, pas de l'application :
/// elle reste dans le controller.
/// </remarks>
public record VerifierIdentifiantsQuery(string Identifiant, string MotDePasse) : IRequest<UtilisateurDto?>;

public class VerifierIdentifiantsQueryHandler(
    IUtilisateurRepository utilisateurRepository,
    IHacheurMotDePasse hacheur
) : IRequestHandler<VerifierIdentifiantsQuery, UtilisateurDto?>
{
    /// <summary>
    /// Empreinte factice, calculée une fois : vérifiée quand l'identifiant est inconnu, pour
    /// que la réponse prenne le même temps qu'un mauvais mot de passe. Sans cela, la durée
    /// de la réponse révélerait quels identifiants existent.
    /// </summary>
    private static string? _empreinteFactice;

    public async Task<UtilisateurDto?> Handle(VerifierIdentifiantsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Identifiant) || string.IsNullOrEmpty(request.MotDePasse))
            return null;

        Utilisateur? utilisateur = await utilisateurRepository.GetByIdentifiantAsync(request.Identifiant);

        if (utilisateur is null)
        {
            _empreinteFactice ??= hacheur.Hacher(Guid.NewGuid().ToString());
            hacheur.Verifier(_empreinteFactice, request.MotDePasse);
            return null;
        }

        return hacheur.Verifier(utilisateur.EmpreinteMotDePasse, request.MotDePasse)
            ? new UtilisateurDto(utilisateur)
            : null;
    }
}
