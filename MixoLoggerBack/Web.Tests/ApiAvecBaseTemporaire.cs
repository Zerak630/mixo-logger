using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Web.Tests;

/// <summary>
/// L'API complète en mémoire, sur sa propre base SQLite dans un fichier temporaire. Chaque
/// fixture est ainsi isolée des autres, et aucun test ne touche la base de développement.
/// </summary>
public class ApiAvecBaseTemporaire : WebApplicationFactory<Program>
{
    private readonly bool _supprimerALaFin;

    /// <param name="fichier">Base à réutiliser (test de redémarrage) ; par défaut un fichier neuf, supprimé à la fin.</param>
    public ApiAvecBaseTemporaire(string? fichier = null)
    {
        _supprimerALaFin = fichier is null;
        Fichier = fichier ?? NouveauFichier();
    }

    public string Fichier { get; }

    public static string NouveauFichier() => Path.Combine(Path.GetTempPath(), $"mixologger-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:MixoLogger", $"Data Source={Fichier}");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        GC.SuppressFinalize(this);

        if (_supprimerALaFin)
            SupprimerBase(Fichier);
    }

    /// <summary>Supprime le fichier et ses journaux WAL. Les connexions en réserve le verrouillent : on les ferme d'abord.</summary>
    public static void SupprimerBase(string fichier)
    {
        SqliteConnection.ClearAllPools();

        foreach (string chemin in new[] { fichier, fichier + "-wal", fichier + "-shm" })
        {
            try { File.Delete(chemin); }
            catch (IOException) { /* Fichier temporaire : tant pis s'il reste. */ }
        }
    }
}
