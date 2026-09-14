using Domain.Utilisateurs;

namespace Application.Utilisateurs;

/// <summary>Ce que le front sait de l'utilisateur connecté. Jamais l'empreinte du mot de passe.</summary>
public record UtilisateurDto(Guid Id, string Identifiant, string NomAffiche)
{
    public UtilisateurDto(Utilisateur utilisateur)
        : this(utilisateur.Id, utilisateur.Identifiant, utilisateur.NomAffiche)
    {
    }
}
