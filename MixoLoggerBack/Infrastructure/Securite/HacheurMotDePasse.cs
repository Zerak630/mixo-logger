using Domain.Interfaces;
using Domain.Utilisateurs;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Securite;

/// <summary>
/// PBKDF2 via le <see cref="PasswordHasher{TUser}"/> d'ASP.NET Core Identity, sans le reste
/// d'Identity : pour 5 comptes déclarés en configuration, le système complet serait
/// surdimensionné (cf. docs/MVP.md §8).
/// </summary>
public class HacheurMotDePasse : IHacheurMotDePasse
{
    private readonly PasswordHasher<Utilisateur> _hacheur = new();

    public string Hacher(string motDePasse)
    {
        ArgumentNullException.ThrowIfNull(motDePasse);

        // Le hacheur n'utilise pas l'utilisateur : le paramètre n'existe que pour Identity.
        return _hacheur.HashPassword(null!, motDePasse);
    }

    public bool Verifier(string empreinte, string motDePasse)
    {
        if (string.IsNullOrEmpty(empreinte) || motDePasse is null)
            return false;

        return _hacheur.VerifyHashedPassword(null!, empreinte, motDePasse) != PasswordVerificationResult.Failed;
    }
}
