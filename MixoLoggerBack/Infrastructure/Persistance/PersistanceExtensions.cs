using Domain.Interfaces.Repositories;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistance;

public static class PersistanceExtensions
{
    /// <summary>Nom de la chaîne de connexion : <c>ConnectionStrings:MixoLogger</c>.</summary>
    public const string NomChaineConnexion = "MixoLogger";

    private const string ChaineParDefaut = "Data Source=Donnees/mixologger.db";

    /// <summary>
    /// Base SQLite et dépôts. Un chemin relatif dans la chaîne de connexion est résolu depuis
    /// <paramref name="racineContenu"/> (le dossier de l'API), pas depuis le dossier courant,
    /// qui change selon la façon de lancer le processus.
    /// </summary>
    public static IServiceCollection AddPersistance(this IServiceCollection services, IConfiguration configuration, string racineContenu)
    {
        string chaine = ResoudreChemin(configuration.GetConnectionString(NomChaineConnexion) ?? ChaineParDefaut, racineContenu);

        services.AddDbContext<MixoLoggerDbContext>(options => options
            .UseSqlite(chaine)
            // Une contrainte violée (nom déjà pris, écriture concurrente) est une issue normale, traduite
            // en 409 par les dépôts : inutile de la journaliser en erreur. Une vraie panne remonte quand
            // même, l'exception étant relancée jusqu'au gestionnaire d'erreurs de l'API.
            .ConfigureWarnings(avertissements => avertissements.Log(
                (CoreEventId.SaveChangesFailed, LogLevel.Debug),
                (RelationalEventId.CommandError, LogLevel.Debug)))
            // Exécutées par Migrate() : le jeu initial n'est inséré que dans une base vide.
            .UseSeeding((contexte, _) => DonneesInitiales.InsererSiVide(contexte))
            .UseAsyncSeeding((contexte, _, annulation) => DonneesInitiales.InsererSiVideAsync(contexte, annulation)));

        services.AddScoped<ICocktailRepository, CocktailRepository>();
        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IBarRepository, BarRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        return services;
    }

    /// <summary>
    /// Crée la base si besoin, applique les migrations en attente puis le jeu initial. Appelée au
    /// démarrage : une base inaccessible ou une migration en échec arrête l'API tout de suite.
    /// </summary>
    public static async Task MettreAJourBaseAsync(this IServiceProvider services, CancellationToken annulation = default)
    {
        await using AsyncServiceScope portee = services.CreateAsyncScope();
        MixoLoggerDbContext db = portee.ServiceProvider.GetRequiredService<MixoLoggerDbContext>();

        string? fichier = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        if (Path.GetDirectoryName(fichier) is { Length: > 0 } dossier)
            Directory.CreateDirectory(dossier);

        await db.Database.MigrateAsync(annulation);

        // Journal WAL : les lectures ne bloquent plus pendant une écriture. Réglage conservé dans le fichier.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", annulation);
    }

    private static string ResoudreChemin(string chaine, string racineContenu)
    {
        SqliteConnectionStringBuilder constructeur = new(chaine);

        if (constructeur.DataSource is { Length: > 0 } source
            && source != ":memory:"
            && constructeur.Mode != SqliteOpenMode.Memory
            && !Path.IsPathRooted(source))
        {
            constructeur.DataSource = Path.GetFullPath(Path.Combine(racineContenu, source));
        }

        return constructeur.ToString();
    }
}
