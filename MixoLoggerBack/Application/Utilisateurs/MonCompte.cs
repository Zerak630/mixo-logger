using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Utilisateurs;
using MediatR;

namespace Application.Utilisateurs;

/// <summary>Le compte de l'utilisateur connecté, lu en base : il reflète un changement d'identifiant ou de nom.</summary>
public record GetMonCompteQuery : IRequest<UtilisateurDto>;

/// <summary>
/// Change l'identifiant de connexion et le nom affiché. Le compte garde son Id : bar, recettes et
/// notes restent les siens. 409 si l'identifiant est déjà pris, 400 s'il est vide.
/// </summary>
public record ModifierMonCompteCommand(string Identifiant, string? NomAffiche) : IRequest<UtilisateurDto>;

/// <summary>
/// Change le mot de passe après vérification de l'actuel. Renvoie le nouveau tampon de sécurité
/// (les autres sessions deviennent invalides), ou <c>null</c> si le mot de passe actuel est faux.
/// </summary>
/// <exception cref="ArgumentException">Nouveau mot de passe trop court, ou identique à l'actuel.</exception>
public record ChangerMotDePasseCommand(string Actuel, string Nouveau) : IRequest<Guid?>;

public class MonCompteHandlers(
    IUtilisateurRepository utilisateurRepository,
    IUtilisateurCourant utilisateurCourant,
    IHacheurMotDePasse hacheur
) : IRequestHandler<GetMonCompteQuery, UtilisateurDto>,
    IRequestHandler<ModifierMonCompteCommand, UtilisateurDto>,
    IRequestHandler<ChangerMotDePasseCommand, Guid?>
{
    public async Task<UtilisateurDto> Handle(GetMonCompteQuery request, CancellationToken cancellationToken) =>
        new(await CompteCourantAsync());

    public async Task<UtilisateurDto> Handle(ModifierMonCompteCommand command, CancellationToken cancellationToken)
    {
        Utilisateur renomme = (await CompteCourantAsync()).Renommer(command.Identifiant ?? string.Empty, command.NomAffiche ?? string.Empty);

        await utilisateurRepository.MettreAJourAsync(renomme);
        return new UtilisateurDto(renomme);
    }

    public async Task<Guid?> Handle(ChangerMotDePasseCommand command, CancellationToken cancellationToken)
    {
        Utilisateur compte = await CompteCourantAsync();

        // Vérifié en premier : sans le mot de passe actuel, on n'apprend rien sur les règles du nouveau.
        if (string.IsNullOrEmpty(command.Actuel) || !hacheur.Verifier(compte.EmpreinteMotDePasse, command.Actuel))
            return null;

        Utilisateur.VerifierMotDePasse(command.Nouveau);
        if (command.Nouveau == command.Actuel)
            throw new ArgumentException("Le nouveau mot de passe doit être différent de l'actuel.", nameof(command));

        Utilisateur modifie = compte.ChangerMotDePasse(hacheur.Hacher(command.Nouveau));
        await utilisateurRepository.MettreAJourAsync(modifie);
        return modifie.TamponSecurite;
    }

    /// <summary>Le compte de la session ; un compte désactivé entre-temps n'est plus reconnu (401).</summary>
    private async Task<Utilisateur> CompteCourantAsync() =>
        await utilisateurRepository.GetByIdAsync(utilisateurCourant.Id)
            ?? throw new UnauthorizedAccessException("Ce compte n'existe plus.");
}
