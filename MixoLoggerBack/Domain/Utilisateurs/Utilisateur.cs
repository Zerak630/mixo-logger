using Domain.Cocktails;
using Domain.Interfaces;

namespace Domain.Utilisateurs;

/// <summary>
/// Un compte de l'application. Pas d'inscription publique : l'administrateur déclare les comptes en
/// configuration, qui les crée en base au démarrage ; ensuite, la base fait foi (cf. docs/MVP.md §8).
/// Immuable : chaque changement produit une nouvelle instance de même <see cref="Id"/>.
/// </summary>
/// <remarks>
/// <para>
/// L'<see cref="Id"/> est conservé en base : c'est lui que référencent les recettes (auteur), le bar
/// (propriétaire) et les notes. Il ne dépend donc plus de l'identifiant de connexion, qui peut
/// changer sans rien faire perdre. À la création seulement, il est dérivé de l'identifiant : un compte
/// déclaré avant que les comptes ne soient en base retrouve ainsi ses données.
/// </para>
/// <para>Le mot de passe n'est jamais conservé en clair, seulement son empreinte.</para>
/// </remarks>
public class Utilisateur : IEntity
{
    /// <summary>Longueur minimale d'un mot de passe, déclaré en configuration ou choisi par l'utilisateur.</summary>
    public const int LongueurMinimaleMotDePasse = 12;

    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Tel que saisi, pour l'affichage ; la comparaison passe par <see cref="IdentifiantNormalise"/>.</summary>
    public string Identifiant { get; }

    /// <summary>Clé de connexion : casse, accents et espaces superflus ignorés.</summary>
    public string IdentifiantNormalise { get; }

    public string NomAffiche { get; }

    public string EmpreinteMotDePasse { get; }

    /// <summary>
    /// Change à chaque changement de mot de passe. Une session ouverte avec un autre tampon est
    /// rejetée : changer son mot de passe ferme les sessions ouvertes ailleurs.
    /// </summary>
    public Guid TamponSecurite { get; }

    /// <summary>Faux pour un compte retiré de la configuration : il ne peut plus se connecter, ses données restent.</summary>
    public bool Actif { get; }

    /// <summary>Nouveau compte : Id dérivé de l'identifiant, cf. remarques.</summary>
    public Utilisateur(string identifiant, string nomAffiche, string empreinteMotDePasse)
        : this(IdentifiantStable(NormaliserObligatoire(identifiant)), identifiant, nomAffiche, empreinteMotDePasse, Guid.NewGuid(), actif: true, DateTime.UtcNow)
    {
    }

    private Utilisateur(Guid id, string identifiant, string nomAffiche, string empreinteMotDePasse, Guid tamponSecurite, bool actif, DateTime creeLe)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(empreinteMotDePasse, nameof(empreinteMotDePasse));
        if (id == Guid.Empty)
            throw new ArgumentException("L'identifiant technique du compte est invalide.", nameof(id));

        IdentifiantNormalise = NormaliserObligatoire(identifiant);
        Identifiant = identifiant.Trim();
        NomAffiche = string.IsNullOrWhiteSpace(nomAffiche) ? Identifiant : nomAffiche.Trim();
        EmpreinteMotDePasse = empreinteMotDePasse;
        Id = id;
        TamponSecurite = tamponSecurite;
        Actif = actif;
        CreatedAt = creeLe;
    }

    /// <summary>Compte relu depuis le stockage, validé comme à la création.</summary>
    public static Utilisateur Reconstituer(Guid id, string identifiant, string nomAffiche, string empreinteMotDePasse, Guid tamponSecurite, bool actif, DateTime creeLe) =>
        new(id, identifiant, nomAffiche, empreinteMotDePasse, tamponSecurite, actif, creeLe);

    /// <summary>Nouvel identifiant de connexion et nom affiché. Même compte : bar, recettes et notes restent les siens.</summary>
    public Utilisateur Renommer(string identifiant, string nomAffiche) =>
        new(Id, identifiant, nomAffiche, EmpreinteMotDePasse, TamponSecurite, Actif, CreatedAt);

    /// <summary>Nouvelle empreinte et nouveau tampon : les sessions ouvertes ailleurs ne sont plus valides.</summary>
    public Utilisateur ChangerMotDePasse(string nouvelleEmpreinte) =>
        new(Id, Identifiant, NomAffiche, nouvelleEmpreinte, Guid.NewGuid(), Actif, CreatedAt);

    public Utilisateur Activer(bool actif) =>
        new(Id, Identifiant, NomAffiche, EmpreinteMotDePasse, TamponSecurite, actif, CreatedAt);

    /// <exception cref="ArgumentException">Mot de passe trop court.</exception>
    public static void VerifierMotDePasse(string? motDePasse)
    {
        if ((motDePasse ?? string.Empty).Length < LongueurMinimaleMotDePasse)
            throw new ArgumentException($"Le mot de passe doit faire au moins {LongueurMinimaleMotDePasse} caractères.", nameof(motDePasse));
    }

    /// <summary>Même règle que pour les noms d'ingrédients : « Alice » et « alice » sont le même compte.</summary>
    public static string Normaliser(string identifiant) => IngredientName.Normalize(identifiant ?? string.Empty);

    private static string NormaliserObligatoire(string identifiant)
    {
        ArgumentNullException.ThrowIfNull(identifiant, nameof(identifiant));

        string normalise = Normaliser(identifiant);
        if (normalise.Length == 0)
            throw new ArgumentException("L'identifiant est obligatoire.", nameof(identifiant));

        return normalise;
    }

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
