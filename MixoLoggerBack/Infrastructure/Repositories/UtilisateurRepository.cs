using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Utilisateurs;
using Infrastructure.Securite;
using Microsoft.Extensions.Options;

namespace Infrastructure.Repositories;

/// <summary>
/// Comptes déclarés en configuration, hachés une fois pour toutes au démarrage.
/// </summary>
/// <remarks>
/// À enregistrer en singleton : le hachage PBKDF2 est volontairement lent, il ne doit pas
/// être refait à chaque requête.
/// </remarks>
public class UtilisateurRepository : IUtilisateurRepository
{
    private readonly Dictionary<string, Utilisateur> _parIdentifiant;
    private readonly Dictionary<Guid, Utilisateur> _parId;

    public UtilisateurRepository(IOptions<ComptesOptions> options, IHacheurMotDePasse hacheur)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hacheur);

        List<Utilisateur> utilisateurs = [.. Valider(options.Value.Liste ?? [])
            .Select(compte => new Utilisateur(compte.Identifiant, compte.NomAffiche ?? string.Empty, hacheur.Hacher(compte.MotDePasse)))];

        _parIdentifiant = utilisateurs.ToDictionary(utilisateur => utilisateur.IdentifiantNormalise);
        _parId = utilisateurs.ToDictionary(utilisateur => utilisateur.Id);
    }

    public int Nombre => _parId.Count;

    public Task<Utilisateur?> GetByIdentifiantAsync(string identifiant) =>
        Task.FromResult(_parIdentifiant.GetValueOrDefault(Utilisateur.Normaliser(identifiant ?? string.Empty)));

    public Task<Utilisateur?> GetByIdAsync(Guid id) =>
        Task.FromResult(_parId.GetValueOrDefault(id));

    /// <summary>
    /// Une configuration invalide arrête le démarrage avec un message explicite, plutôt que de
    /// laisser un compte inutilisable ou un mot de passe faible passer inaperçu.
    /// </summary>
    private static IEnumerable<CompteConfigure> Valider(IReadOnlyList<CompteConfigure> comptes)
    {
        HashSet<string> vus = [];

        for (int i = 0; i < comptes.Count; i++)
        {
            CompteConfigure compte = comptes[i];
            string ou = $"{ComptesOptions.Section}:{i}";

            if (string.IsNullOrWhiteSpace(compte.Identifiant) || Utilisateur.Normaliser(compte.Identifiant).Length == 0)
                throw new InvalidOperationException($"Configuration « {ou} » : l'identifiant est obligatoire.");

            if ((compte.MotDePasse ?? string.Empty).Length < ComptesOptions.LongueurMinimaleMotDePasse)
                throw new InvalidOperationException(
                    $"Configuration « {ou} » ({compte.Identifiant}) : le mot de passe doit faire au moins {ComptesOptions.LongueurMinimaleMotDePasse} caractères.");

            if (!vus.Add(Utilisateur.Normaliser(compte.Identifiant)))
                throw new InvalidOperationException(
                    $"Configuration « {ou} » : l'identifiant « {compte.Identifiant} » est déclaré plusieurs fois (casse et accents ignorés).");

            yield return compte;
        }
    }
}
