using Infrastructure.Persistance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>Schéma, migrations et démarrage de la base (F10).</summary>
public class SchemaTests : BaseDeTest
{
    [Fact]
    public async Task LeModele_NaPasDeChangementSansMigration()
    {
        // Échoue si quelqu'un modifie un modèle de stockage ou le DbContext sans générer de migration :
        // l'API démarrerait alors sur un schéma qui ne correspond plus au code.
        bool enAttente = await DansLaBaseAsync(db => Task.FromResult(db.Database.HasPendingModelChanges()));

        Assert.False(enAttente, "Le modèle a changé : générer une migration (cf. docs/MVP.md §10.2).");
    }

    [Fact]
    public async Task ToutesLesMigrations_SontAppliquees()
    {
        var (connues, appliquees) = await DansLaBaseAsync(async db =>
            (db.Database.GetMigrations().ToList(), (await db.Database.GetAppliedMigrationsAsync(Jeton)).ToList()));

        Assert.NotEmpty(connues);
        Assert.Equal(connues, appliquees);
    }

    [Fact]
    public async Task MiseAJour_EstIdempotente_EtNeReinserePasLeJeuInitial()
    {
        async Task<(int Ingredients, int Alias, int Cocktails, int Etapes)> CompterAsync() => await DansLaBaseAsync(async db => (
            await db.Ingredients.CountAsync(Jeton),
            await db.Alias.CountAsync(Jeton),
            await db.Cocktails.CountAsync(Jeton),
            await db.Etapes.CountAsync(Jeton)));

        var avant = await CompterAsync();
        await Services.MettreAJourBaseAsync(Jeton);
        await Services.MettreAJourBaseAsync(Jeton);

        Assert.Equal(avant, await CompterAsync());
    }

    [Fact]
    public async Task JeuInitial_ContenuAttendu()
    {
        var (ingredients, recettes, auteurs) = await DansLaBaseAsync(async db => (
            await db.Ingredients.CountAsync(Jeton),
            await db.Cocktails.Select(c => c.Nom).OrderBy(n => n).ToListAsync(Jeton),
            await db.Cocktails.CountAsync(c => c.AuteurId != null, Jeton)));

        // 30 ingrédients déclarés, plus les 3 que les recettes introduisent (jus de tomate, Worcestershire, Tabasco).
        Assert.Equal(33, ingredients);
        Assert.Equal(["Bloody Mary", "Cosmopolitan", "Mojito", "Piña Colada", "Tequila Sunrise"], recettes);
        Assert.Equal(0, auteurs);
    }

    [Fact]
    public async Task JeuInitial_AucunAliasNeReprendUnNomCanonique()
    {
        var conflits = await DansLaBaseAsync(db => db.Alias
            .Where(alias => db.Ingredients.Any(ingredient => ingredient.NomNormalise == alias.Alias))
            .Select(alias => alias.Alias)
            .ToListAsync(Jeton));

        Assert.Empty(conflits);
    }

    [Fact]
    public async Task LesClesEtrangeres_SontAppliquees()
    {
        // SQLite ne les applique que si la connexion l'active : sans elles, ni cascade ni refus.
        long active = await DansLaBaseAsync(async db =>
        {
            await db.Database.OpenConnectionAsync(Jeton);
            await using var commande = db.Database.GetDbConnection().CreateCommand();
            commande.CommandText = "PRAGMA foreign_keys;";
            return (long)(await commande.ExecuteScalarAsync(Jeton))!;
        });

        Assert.Equal(1, active);
    }

    [Fact]
    public async Task LeJournal_EstEnModeWal()
    {
        string mode = await DansLaBaseAsync(async db =>
        {
            await db.Database.OpenConnectionAsync(Jeton);
            await using var commande = db.Database.GetDbConnection().CreateCommand();
            commande.CommandText = "PRAGMA journal_mode;";
            return (string)(await commande.ExecuteScalarAsync(Jeton))!;
        });

        Assert.Equal("wal", mode);
    }

    [Fact]
    public async Task CheminRelatif_EstResoluDepuisLaRacineDeLApi_PasDepuisLeDossierCourant()
    {
        string racine = Path.Combine(Path.GetTempPath(), $"mixologger-racine-{Guid.NewGuid():N}");

        try
        {
            await using (ServiceProvider services = Construire("Data Source=sous/dossier/base.db", racine))
            {
                await services.MettreAJourBaseAsync(Jeton);
            }

            Assert.True(File.Exists(Path.Combine(racine, "sous", "dossier", "base.db")));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(racine, recursive: true); } catch (IOException) { }
        }
    }
}
