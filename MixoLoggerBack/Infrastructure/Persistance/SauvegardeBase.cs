using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistance;

/// <summary>Section de configuration <c>Sauvegarde</c>.</summary>
public class SauvegardeOptions
{
    public const string Section = "Sauvegarde";

    /// <summary>Faux pour ne jamais sauvegarder (tests). Une base en mémoire n'est jamais sauvegardée.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Dossier des copies, déjà résolu. Par défaut, <c>Sauvegardes</c> à côté de la base.</summary>
    public string Dossier { get; set; } = string.Empty;

    /// <summary>Âge maximal de la dernière copie automatique avant d'en faire une nouvelle.</summary>
    public TimeSpan Intervalle { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Nombre de copies gardées, pour chaque motif ; les plus anciennes sont supprimées.</summary>
    public int Conservation { get; set; } = 7;
}

/// <summary>
/// Copies de la base SQLite par <c>VACUUM INTO</c> : une copie cohérente, prise sans arrêter l'API
/// (journal WAL compris), compacte, et qui s'ouvre telle quelle. Restaurer une copie, c'est la mettre
/// à la place de la base, API arrêtée (cf. docs/MVP.md §10.2).
/// </summary>
/// <remarks>
/// Nom d'une copie : <c>{base}-{aaaaMMjj-HHmmss-fff}-{motif}.db</c>, en heure UTC. Le nom porte la
/// date : c'est lui, et non la date du fichier, qui dit quand la copie a été faite.
/// </remarks>
public sealed class SauvegardeBase(SauvegardeOptions options, string fichierBase, IServiceScopeFactory portees, TimeProvider horloge, ILogger<SauvegardeBase> journal)
{
    /// <summary>Copie périodique.</summary>
    public const string MotifAutomatique = "auto";

    /// <summary>Copie prise juste avant d'appliquer des migrations à une base existante.</summary>
    public const string MotifAvantMigration = "avant-migration";

    private const string FormatDate = "yyyyMMdd-HHmmss-fff";

    public SauvegardeOptions Options => options;

    private string NomBase => Path.GetFileNameWithoutExtension(fichierBase);

    /// <summary>Copie la base maintenant, puis supprime les copies de même motif au-delà de la conservation.</summary>
    /// <returns>Le chemin de la copie.</returns>
    public async Task<string> SauvegarderAsync(string motif, CancellationToken annulation = default)
    {
        Directory.CreateDirectory(options.Dossier);
        string copie = Path.Combine(options.Dossier,
            $"{NomBase}-{horloge.GetUtcNow().UtcDateTime.ToString(FormatDate, CultureInfo.InvariantCulture)}-{motif}.db");

        await using (AsyncServiceScope portee = portees.CreateAsyncScope())
        {
            MixoLoggerDbContext db = portee.ServiceProvider.GetRequiredService<MixoLoggerDbContext>();
            await db.Database.ExecuteSqlAsync($"VACUUM INTO {copie}", annulation);
        }

        // Sans cela, la connexion en réserve garde la copie ouverte (verrou sous Windows).
        SqliteConnection.ClearAllPools();

        foreach (string ancienne in Copies(motif).Skip(Math.Max(1, options.Conservation)))
        {
            try { File.Delete(ancienne); }
            catch (IOException erreur) { journal.LogWarning(erreur, "Ancienne sauvegarde non supprimée : {Copie}", ancienne); }
        }

        journal.LogInformation("Base sauvegardée : {Copie}", copie);
        return copie;
    }

    /// <summary>Copie automatique si la dernière est plus vieille que l'intervalle (ou s'il n'y en a pas).</summary>
    /// <returns>Le chemin de la copie, ou <c>null</c> si elle n'était pas nécessaire.</returns>
    public async Task<string?> SauvegarderSiNecessaireAsync(CancellationToken annulation = default)
    {
        if (!options.Active)
            return null;

        DateTime? derniere = DateDeLaDerniere(MotifAutomatique);
        if (derniere is { } date && horloge.GetUtcNow().UtcDateTime - date < options.Intervalle)
            return null;

        return await SauvegarderAsync(MotifAutomatique, annulation);
    }

    /// <summary>Date (UTC) de la copie la plus récente pour ce motif, lue dans son nom.</summary>
    public DateTime? DateDeLaDerniere(string motif) =>
        Copies(motif).Select(copie => DateDe(copie, motif)).FirstOrDefault(date => date is not null);

    /// <summary>Copies de ce motif, de la plus récente à la plus ancienne.</summary>
    public IReadOnlyList<string> Copies(string motif) =>
        Directory.Exists(options.Dossier)
            ? [.. Directory.EnumerateFiles(options.Dossier, $"{NomBase}-*-{motif}.db")
                .Where(copie => DateDe(copie, motif) is not null)
                .OrderByDescending(copie => DateDe(copie, motif))]
            : [];

    private DateTime? DateDe(string copie, string motif)
    {
        string nom = Path.GetFileNameWithoutExtension(copie);
        string prefixe = $"{NomBase}-", suffixe = $"-{motif}";

        if (!nom.StartsWith(prefixe, StringComparison.Ordinal) || !nom.EndsWith(suffixe, StringComparison.Ordinal))
            return null;

        return DateTime.TryParseExact(nom[prefixe.Length..^suffixe.Length], FormatDate, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime date)
            ? date
            : null;
    }
}
