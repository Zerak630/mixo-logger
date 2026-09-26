using Infrastructure.Persistance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Web.Tests;

/// <summary>La sauvegarde automatique tourne avec l'API, sans rien demander.</summary>
public class SauvegardeApiTests
{
    private sealed class ApiAvecSauvegardes(string dossier) : ApiAvecBaseTemporaire
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Sauvegarde:Active", "true");
            builder.UseSetting("Sauvegarde:Dossier", dossier);
        }
    }

    [Fact]
    public async Task AuDemarrage_SansCopieRecente_LaBaseEstSauvegardee()
    {
        string dossier = Path.Combine(Path.GetTempPath(), $"mixologger-sauvegardes-{Guid.NewGuid():N}");

        try
        {
            await using var api = new ApiAvecSauvegardes(dossier);
            api.CreateClient();
            SauvegardeBase sauvegarde = api.Services.GetRequiredService<SauvegardeBase>();

            // Le service tourne en arrière-plan : on lui laisse quelques secondes.
            for (int essai = 0; essai < 50 && sauvegarde.Copies(SauvegardeBase.MotifAutomatique).Count == 0; essai++)
                await Task.Delay(100, TestContext.Current.CancellationToken);

            Assert.Single(sauvegarde.Copies(SauvegardeBase.MotifAutomatique));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(dossier, recursive: true); } catch (IOException) { }
        }
    }
}
