using Domain.Cocktails;
using Domain.Interfaces;

namespace Domain.Utilisateurs;

/// <summary>
/// Un compte de l'application. Pas d'inscription publique : les comptes sont déclarés en
/// configuration par l'administrateur (cf. docs/MVP.md §8).
/// </summary>
/// <remarks>
/// Le mot de passe n'est jamais conservé en clair, seulement son empreinte. L'<see cref="Id"/>
/// est dérivé de l'identifiant normalisé : il reste stable d'un redémarrage à l'autre, ce qui
/// compte dès que des données (auteur d'une recette, propriétaire d'un bar — lot C) y feront
/// référence, et tant que les comptes ne sont pas persistés.
/// </remarks>
public class Utilisateur : IEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Tel que déclaré, pour l'affichage technique ; la comparaison passe par <see cref="IdentifiantNormalise"/>.</summary>
    public string Identifiant { get; }

    /// <summary>Clé de connexion : casse, accents et espaces superflus ignorés.</summary>
    public string IdentifiantNormalise { get; }

    public string NomAffiche { get; }

    public string EmpreinteMotDePasse { get; }

    public Utilisateur(string identifiant, string nomAffiche, string empreinteMotDePasse)
    {
        ArgumentNullException.ThrowIfNull(identifiant, nameof(identifiant));
        ArgumentException.ThrowIfNullOrWhiteSpace(empreinteMotDePasse, nameof(empreinteMotDePasse));

        IdentifiantNormalise = Normaliser(identifiant);
        if (IdentifiantNormalise.Length == 0)
            throw new ArgumentException("L'identifiant est obligatoire.", nameof(identifiant));

        Identifiant = identifiant.Trim();
        NomAffiche = string.IsNullOrWhiteSpace(nomAffiche) ? Identifiant : nomAffiche.Trim();
        EmpreinteMotDePasse = empreinteMotDePasse;
        Id = IdentifiantStable(IdentifiantNormalise);
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Même règle que pour les noms d'ingrédients : « Alice » et « alice » sont le même compte.</summary>
    public static string Normaliser(string identifiant) => IngredientName.Normalize(identifiant ?? string.Empty);

    /// <summary>GUID déterministe (version 5, RFC 9562) : même identifiant, même Id.</summary>
    private static Guid IdentifiantStable(string identifiantNormalise)
    {
        byte[] espace = Guid.Parse("8f5f6a6e-4c1b-4f55-9a2e-6d1d6f0b7c31").ToByteArray(bigEndian: true);
        byte[] nom = System.Text.Encoding.UTF8.GetBytes(identifiantNormalise);
        byte[] empreinte = System.Security.Cryptography.SHA1.HashData([.. espace, .. nom]);

        empreinte[6] = (byte)((empreinte[6] & 0x0F) | 0x50);
        empreinte[8] = (byte)((empreinte[8] & 0x3F) | 0x80);

        return new Guid(empreinte.AsSpan(0, 16), bigEndian: true);
    }
}
