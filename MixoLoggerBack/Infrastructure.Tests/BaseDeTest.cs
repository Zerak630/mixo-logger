using Domain.Cocktails;
using Infrastructure.Persistance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Une base SQLite neuve par test, montée exactement comme au démarrage de l'API : migrations,
/// jeu initial, journal WAL. Chaque <see cref="Portee"/> imite une requête (DbContext et dépôts neufs).
/// </summary>
public abstract class BaseDeTest : IAsyncLifetime
{
    protected static CancellationToken Jeton => TestContext.Current.CancellationToken;

    private readonly string _dossier = Path.Combine(Path.GetTempPath(), $"mixologger-infra-{Guid.NewGuid():N}");
    private ServiceProvider? _services;

    protected string Fichier => Path.Combine(_dossier, "test.db");

    protected ServiceProvider Services => _services ?? throw new InvalidOperationException("Base non initialisée.");

    public virtual async ValueTask InitializeAsync()
    {
        _services = Construire("Data Source=test.db", _dossier);
        await _services.MettreAJourBaseAsync(Jeton);
    }

    /// <summary>Les services de persistance tels qu'enregistrés par l'API, sur la chaîne donnée.</summary>
    protected static ServiceProvider Construire(string chaine, string racineContenu)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:MixoLogger"] = chaine })
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddPersistance(configuration, racineContenu)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    /// <summary>L'équivalent d'une requête HTTP : ses propres DbContext et dépôts.</summary>
    protected AsyncServiceScope Portee() => Services.CreateAsyncScope();

    protected async Task<T> AvecAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        await using AsyncServiceScope portee = Portee();
        return await action(portee.ServiceProvider.GetRequiredService<TService>());
    }

    protected async Task AvecAsync<TService>(Func<TService, Task> action) where TService : notnull
    {
        await using AsyncServiceScope portee = Portee();
        await action(portee.ServiceProvider.GetRequiredService<TService>());
    }

    /// <summary>Lecture directe des tables, pour vérifier ce que l'API ne montre pas (lignes orphelines…).</summary>
    protected Task<T> DansLaBaseAsync<T>(Func<MixoLoggerDbContext, Task<T>> lecture) =>
        AvecAsync<MixoLoggerDbContext, T>(db => lecture(db));

    protected static string Unique(string prefixe) => $"{prefixe} {Guid.NewGuid():N}";

    protected static CocktailIngredient Composant(Ingredient ingredient, double valeur, string unite) =>
        new(ingredient, new Dose(valeur, unite));

    public virtual async ValueTask DisposeAsync()
    {
        if (_services is not null)
            await _services.DisposeAsync();

        // Une connexion d'un test voisin peut encore verrouiller le fichier un court instant : on réessaie.
        for (int essai = 0; essai < 10 && Directory.Exists(_dossier); essai++)
        {
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(_dossier, recursive: true); }
            catch (IOException) { await Task.Delay(100); }
            catch (UnauthorizedAccessException) { await Task.Delay(100); }
        }

        GC.SuppressFinalize(this);
    }
}
