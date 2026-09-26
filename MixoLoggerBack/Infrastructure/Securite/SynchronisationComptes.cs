using Domain.Interfaces;
using Domain.Utilisateurs;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Securite;

/// <summary>Ce que le démarrage a fait des comptes déclarés en configuration.</summary>
public record BilanComptes(int Crees, int Reactives, int Reinitialises, int Desactives, int Actifs);

/// <summary>
/// Accorde les comptes en base avec la section <c>Comptes</c>, au démarrage (cf. <see cref="ComptesOptions"/>).
/// </summary>
public static class SynchronisationComptes
{
    /// <summary>
    /// Crée les comptes des entrées nouvelles, réactive ceux qui reviennent, désactive ceux dont
    /// l'entrée a disparu et réinitialise les mots de passe demandés. Une configuration invalide
    /// arrête le démarrage avec un message explicite.
    /// </summary>
    /// <exception cref="InvalidOperationException">Configuration invalide : rien n'est enregistré.</exception>
    public static async Task<BilanComptes> SynchroniserComptesAsync(
        this IServiceProvider services,
        IReadOnlyList<CompteConfigure> comptes,
        IHacheurMotDePasse hacheur,
        CancellationToken annulation = default)
    {
        ArgumentNullException.ThrowIfNull(hacheur);
        List<(CompteConfigure Compte, string Cle, string Ou)> entrees = ValiderStructure(comptes ?? []);

        await using AsyncServiceScope portee = services.CreateAsyncScope();
        MixoLoggerDbContext db = portee.ServiceProvider.GetRequiredService<MixoLoggerDbContext>();
        List<CompteDonnees> existants = await db.Comptes.ToListAsync(annulation);

        int crees = 0, reactives = 0, reinitialises = 0, desactives = 0;

        foreach ((CompteConfigure entree, string cle, string ou) in entrees)
        {
            CompteDonnees? compte = existants.FirstOrDefault(c => c.CleConfiguration == cle);

            if (compte is null)
            {
                VerifierMotDePasse(entree, ou);

                // Un titulaire a pu prendre cet identifiant en changeant le sien : deux comptes ne
                // peuvent pas se partager une clé de connexion.
                if (existants.Any(c => c.IdentifiantNormalise == cle))
                    throw new InvalidOperationException(
                        $"Configuration « {ou} » : l'identifiant « {entree.Identifiant} » est déjà utilisé par un autre compte.");

                Utilisateur nouveau = new(entree.Identifiant, entree.NomAffiche ?? string.Empty, hacheur.Hacher(entree.MotDePasse!));
                if (existants.Any(c => c.Id == nouveau.Id))
                    throw new InvalidOperationException(
                        $"Configuration « {ou} » : le compte « {entree.Identifiant} » entre en conflit avec un compte existant.");

                CompteDonnees donnees = new()
                {
                    Id = nouveau.Id,
                    Identifiant = nouveau.Identifiant,
                    IdentifiantNormalise = nouveau.IdentifiantNormalise,
                    NomAffiche = nouveau.NomAffiche,
                    EmpreinteMotDePasse = nouveau.EmpreinteMotDePasse,
                    TamponSecurite = nouveau.TamponSecurite,
                    Actif = true,
                    CreeLe = nouveau.CreatedAt,
                    CleConfiguration = cle
                };
                db.Comptes.Add(donnees);
                existants.Add(donnees);
                crees++;
                continue;
            }

            if (!compte.Actif)
            {
                compte.Actif = true;
                reactives++;
            }

            if (entree.ReinitialiserMotDePasse)
            {
                VerifierMotDePasse(entree, ou);
                compte.Appliquer(compte.VersDomaine().ChangerMotDePasse(hacheur.Hacher(entree.MotDePasse!)));
                reinitialises++;
            }
        }

        HashSet<string> cles = [.. entrees.Select(entree => entree.Cle)];
        foreach (CompteDonnees compte in existants.Where(c => c.Actif && c.CleConfiguration is { } cle && !cles.Contains(cle)))
        {
            compte.Actif = false;
            desactives++;
        }

        await db.SaveChangesAsync(annulation);

        return new BilanComptes(crees, reactives, reinitialises, desactives, existants.Count(c => c.Actif));
    }

    /// <summary>Ce qui se vérifie sans la base : identifiants présents et distincts.</summary>
    private static List<(CompteConfigure, string, string)> ValiderStructure(IReadOnlyList<CompteConfigure> comptes)
    {
        List<(CompteConfigure, string, string)> entrees = [];
        HashSet<string> vus = [];

        for (int i = 0; i < comptes.Count; i++)
        {
            CompteConfigure compte = comptes[i];
            string ou = $"{ComptesOptions.Section}:{i}";
            string cle = Utilisateur.Normaliser(compte.Identifiant ?? string.Empty);

            if (cle.Length == 0)
                throw new InvalidOperationException($"Configuration « {ou} » : l'identifiant est obligatoire.");

            if (!vus.Add(cle))
                throw new InvalidOperationException(
                    $"Configuration « {ou} » : l'identifiant « {compte.Identifiant} » est déclaré plusieurs fois (casse et accents ignorés).");

            entrees.Add((compte, cle, ou));
        }

        return entrees;
    }

    private static void VerifierMotDePasse(CompteConfigure compte, string ou)
    {
        if ((compte.MotDePasse ?? string.Empty).Length < ComptesOptions.LongueurMinimaleMotDePasse)
            throw new InvalidOperationException(
                $"Configuration « {ou} » ({compte.Identifiant}) : le mot de passe doit faire au moins {ComptesOptions.LongueurMinimaleMotDePasse} caractères.");
    }
}
