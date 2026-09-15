using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistance;

/// <summary>
/// Utilisée uniquement par <c>dotnet ef</c> pour générer les migrations, sans démarrer l'API
/// (qui exigerait des comptes configurés). Aucune connexion n'est ouverte à la génération.
/// </summary>
public class MixoLoggerDbContextFactory : IDesignTimeDbContextFactory<MixoLoggerDbContext>
{
    public MixoLoggerDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<MixoLoggerDbContext>().UseSqlite("Data Source=conception.db").Options);
}
