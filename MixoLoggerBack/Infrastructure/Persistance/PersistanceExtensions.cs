using Domain.Interfaces.Repositories;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        services.AddSingleton(LireOptionsSauvegarde(configuration, chaine, racineContenu));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(fournisseur => new SauvegardeBase(
            fournisseur.GetRequiredService<SauvegardeOptions>(),
            new SqliteConnectionStringBuilder(chaine).DataSource,
            fournisseur.GetRequiredService<IServiceScopeFactory>(),
            fournisseur.GetRequiredService<TimeProvider>(),
            fournisseur.GetRequiredService<ILogger<SauvegardeBase>>()));

        return services;
    }

    /// <summary>
    /// Section <c>Sauvegarde</c> : <c>Active</c>, <c>Dossier</c> (relatif au dossier de l'API ; par défaut
    /// <c>Sauvegardes</c> à côté de la base), <c>IntervalleHeures</c>, <c>Conservation</c>.
    /// </summary>
    private static SauvegardeOptions LireOptionsSauvegarde(IConfiguration configuration, string chaine, string racineContenu)
    {
        IConfigurationSection section = configuration.GetSection(SauvegardeOptions.Section);
        SqliteConnectionStringBuilder constructeur = new(chaine);
        bool enMemoire = constructeur.DataSource is "" or ":memory:" || constructeur.Mode == SqliteOpenMode.Memory;

        string dossier = section["Dossier"] is { Length: > 0 } configure
            ? Path.GetFullPath(Path.Combine(racineContenu, configure))
            : Path.Combine(Path.GetDirectoryName(constructeur.DataSource) ?? racineContenu, "Sauvegardes");

        return new SauvegardeOptions
        {
            Active = !enMemoire && !string.Equals(section["Active"], "false", StringComparison.OrdinalIgnoreCase),
            Dossier = dossier,
            Intervalle = double.TryParse(section["IntervalleHeures"], System.Globalization.CultureInfo.InvariantCulture, out double heures) && heures > 0
                ? TimeSpan.FromHours(heures)
                : TimeSpan.FromHours(24),
            Conservation = int.TryParse(section["Conservation"], out int nombre) && nombre > 0 ? nombre : 7
        };
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

        // Une migration peut transformer ou supprimer des données : on garde la base telle qu'elle était.
        // Pas pour une base neuve, qui n'a rien à perdre.
        SauvegardeBase? sauvegarde = portee.ServiceProvider.GetService<SauvegardeBase>();
        if (sauvegarde is { Options.Active: true }
            && File.Exists(fichier)
            && (await db.Database.GetAppliedMigrationsAsync(annulation)).Any()
            && (await db.Database.GetPendingMigrationsAsync(annulation)).Any())
        {
            await sauvegarde.SauvegarderAsync(SauvegardeBase.MotifAvantMigration, annulation);
        }

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
