using Domain.Cocktails;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>Sauvegardes de la base (VACUUM INTO) : contenu, rotation, cadence, copie avant migration.</summary>
public class SauvegardeTests : BaseDeTest
{
    /// <summary>Horloge réglable : les copies sont datées par leur nom, à la milliseconde.</summary>
    private sealed class Horloge(DateTimeOffset depart) : TimeProvider
    {
        public DateTimeOffset Maintenant { get; set; } = depart;
        public override DateTimeOffset GetUtcNow() => Maintenant;
    }

    private readonly Horloge _horloge = new(new DateTimeOffset(2026, 9, 26, 8, 0, 0, TimeSpan.Zero));

    private string DossierSauvegardes => Path.Combine(Dossier, "Sauvegardes");

    private SauvegardeBase Service(int conservation = 7, TimeSpan? intervalle = null) => new(
        new SauvegardeOptions { Dossier = DossierSauvegardes, Conservation = conservation, Intervalle = intervalle ?? TimeSpan.FromHours(24) },
        Fichier,
        Services.GetRequiredService<IServiceScopeFactory>(),
        _horloge,
        NullLogger<SauvegardeBase>.Instance);

    private static async Task<long> CompterAsync(string fichier, string requete)
    {
        await using SqliteConnection connexion = new($"Data Source={fichier};Mode=ReadOnly;Pooling=False");
        await connexion.OpenAsync(Jeton);
        await using SqliteCommand commande = connexion.CreateCommand();
        commande.CommandText = requete;
        return Convert.ToInt64(await commande.ExecuteScalarAsync(Jeton));
    }

    [Fact]
    public async Task Copie_ContientLesDonnees_EtSOuvreTelleQuelle()
    {
        string nom = Unique("Sauvegardé");
        Ingredient gin = await AvecAsync<IIngredientRepository, Ingredient>(depot => depot.GetOrCreateAsync("Gin"));
        await AvecAsync<ICocktailRepository>(depot => depot.AddAsync(new Cocktail(nom, [Composant(gin, 4, "cL")], EtapeRecette.FromOrderedList(["Verser"]))));

        string copie = await Service().SauvegarderAsync(SauvegardeBase.MotifAutomatique, Jeton);

        Assert.Equal(Path.Combine(DossierSauvegardes, "test-20260926-080000-000-auto.db"), copie);
        Assert.Equal(1, await CompterAsync(copie, $"SELECT COUNT(*) FROM Cocktails WHERE Nom = '{nom}'"));
        Assert.Equal(1, await CompterAsync(copie, "SELECT COUNT(*) FROM pragma_integrity_check WHERE integrity_check = 'ok'"));
    }

    [Fact]
    public async Task Rotation_GardeLesPlusRecentes_MotifParMotif()
    {
        SauvegardeBase service = Service(conservation: 2);
        string avantMigration = await service.SauvegarderAsync(SauvegardeBase.MotifAvantMigration, Jeton);

        List<string> automatiques = [];
        for (int jour = 0; jour < 4; jour++)
        {
            _horloge.Maintenant = _horloge.Maintenant.AddDays(1);
            automatiques.Add(await service.SauvegarderAsync(SauvegardeBase.MotifAutomatique, Jeton));
        }

        Assert.Equal([automatiques[3], automatiques[2]], service.Copies(SauvegardeBase.MotifAutomatique));
        Assert.False(File.Exists(automatiques[0]));
        // Les copies d'un autre motif ne comptent pas dans la rotation des copies automatiques.
        Assert.True(File.Exists(avantMigration));
    }

    [Fact]
    public async Task SiNecessaire_SeulementQuandLaDerniereADepasseLIntervalle()
    {
        SauvegardeBase service = Service(intervalle: TimeSpan.FromHours(24));

        Assert.NotNull(await service.SauvegarderSiNecessaireAsync(Jeton));

        _horloge.Maintenant = _horloge.Maintenant.AddHours(23);
        Assert.Null(await service.SauvegarderSiNecessaireAsync(Jeton));

        _horloge.Maintenant = _horloge.Maintenant.AddHours(1);
        Assert.NotNull(await service.SauvegarderSiNecessaireAsync(Jeton));
        Assert.Equal(new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc), service.DateDeLaDerniere(SauvegardeBase.MotifAutomatique));
    }

    [Fact]
    public async Task Desactivee_NeSauvegardeRien()
    {
        SauvegardeBase service = new(
            new SauvegardeOptions { Active = false, Dossier = DossierSauvegardes },
            Fichier, Services.GetRequiredService<IServiceScopeFactory>(), _horloge, NullLogger<SauvegardeBase>.Instance);

        Assert.Null(await service.SauvegarderSiNecessaireAsync(Jeton));
        Assert.False(Directory.Exists(DossierSauvegardes));
    }

    [Fact]
    public void BaseNeuve_AucuneCopieAvantMigration()
    {
        // La base de test vient d'être créée et migrée par InitializeAsync.
        Assert.False(Directory.Exists(DossierSauvegardes));
    }

    [Fact]
    public async Task BaseExistante_AvecMigrationsEnAttente_EstCopieeAvant()
    {
        string racine = Path.Combine(Dossier, "ancienne");

        await using (ServiceProvider services = Construire("Data Source=base.db", racine))
        {
            Directory.CreateDirectory(racine);
            await using (AsyncServiceScope portee = services.CreateAsyncScope())
            {
                MixoLoggerDbContext db = portee.ServiceProvider.GetRequiredService<MixoLoggerDbContext>();
                await db.GetService<IMigrator>().MigrateAsync("20260926094612_PhotoCocktail", Jeton);
            }

            await services.MettreAJourBaseAsync(Jeton);

            SauvegardeBase service = services.GetRequiredService<SauvegardeBase>();
            string copie = Assert.Single(service.Copies(SauvegardeBase.MotifAvantMigration));
            Assert.Equal(Path.Combine(racine, "Sauvegardes"), Path.GetDirectoryName(copie));
            // La copie est la base d'avant : la dernière migration n'y est pas appliquée.
            Assert.Equal(0, await CompterAsync(copie, "SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId > '20260926094612_PhotoCocktail'"));

            // Plus rien en attente : un nouveau démarrage ne recopie pas.
            await services.MettreAJourBaseAsync(Jeton);
            Assert.Single(service.Copies(SauvegardeBase.MotifAvantMigration));
        }
        SqliteConnection.ClearAllPools();
    }

    [Fact]
    public async Task Options_DossierParDefautACoteDeLaBase_DossierConfigureRelatifALaRacine_MemoireJamaisSauvegardee()
    {
        await using (ServiceProvider parDefaut = Construire("Data Source=donnees/base.db", Dossier))
            Assert.Equal(Path.Combine(Dossier, "donnees", "Sauvegardes"), parDefaut.GetRequiredService<SauvegardeOptions>().Dossier);

        await using (ServiceProvider configure = Construire("Data Source=base.db", Dossier, new Dictionary<string, string?>
        {
            ["Sauvegarde:Dossier"] = "copies",
            ["Sauvegarde:IntervalleHeures"] = "6",
            ["Sauvegarde:Conservation"] = "3"
        }))
        {
            SauvegardeOptions options = configure.GetRequiredService<SauvegardeOptions>();
            Assert.Equal(Path.Combine(Dossier, "copies"), options.Dossier);
            Assert.Equal(TimeSpan.FromHours(6), options.Intervalle);
            Assert.Equal(3, options.Conservation);
            Assert.True(options.Active);
        }

        await using (ServiceProvider memoire = Construire("Data Source=:memory:", Dossier))
            Assert.False(memoire.GetRequiredService<SauvegardeOptions>().Active);
    }
}
