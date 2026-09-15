using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance;

/// <summary>
/// Base SQLite de l'application (F10). Les comptes n'y sont pas : ils restent déclarés en
/// configuration (cf. docs/MVP.md §8).
/// </summary>
public class MixoLoggerDbContext(DbContextOptions<MixoLoggerDbContext> options) : DbContext(options)
{
    public DbSet<IngredientDonnees> Ingredients => Set<IngredientDonnees>();
    public DbSet<AliasDonnees> Alias => Set<AliasDonnees>();
    public DbSet<CocktailDonnees> Cocktails => Set<CocktailDonnees>();
    public DbSet<ComposantDonnees> Composants => Set<ComposantDonnees>();
    public DbSet<EtapeDonnees> Etapes => Set<EtapeDonnees>();
    public DbSet<BarDonnees> Bars => Set<BarDonnees>();
    public DbSet<LigneStockDonnees> LignesStock => Set<LigneStockDonnees>();
    public DbSet<NoteDonnees> Notes => Set<NoteDonnees>();

    protected override void OnModelCreating(ModelBuilder modele)
    {
        modele.Entity<IngredientDonnees>(ingredient =>
        {
            ingredient.ToTable("Ingredients");
            ingredient.HasKey(i => i.Id);
            ingredient.Property(i => i.Nom).HasMaxLength(200);
            ingredient.Property(i => i.NomNormalise).HasMaxLength(200);
            ingredient.HasIndex(i => i.NomNormalise).IsUnique();
            ingredient.HasMany(i => i.Alias).WithOne().HasForeignKey(a => a.IngredientId).OnDelete(DeleteBehavior.Cascade);
        });

        modele.Entity<AliasDonnees>(alias =>
        {
            alias.ToTable("AliasIngredients");
            alias.HasKey(a => a.Alias);
            alias.Property(a => a.Alias).HasMaxLength(200);
        });

        modele.Entity<CocktailDonnees>(cocktail =>
        {
            cocktail.ToTable("Cocktails");
            cocktail.HasKey(c => c.Id);
            cocktail.Property(c => c.Nom).HasMaxLength(200);
            cocktail.Property(c => c.NomNormalise).HasMaxLength(200);
            cocktail.HasIndex(c => c.NomNormalise).IsUnique();
            cocktail.Property(c => c.Description).HasMaxLength(2000);
            cocktail.HasIndex(c => c.AuteurId);
            cocktail.HasMany(c => c.Composants).WithOne().HasForeignKey(c => c.CocktailId).OnDelete(DeleteBehavior.Cascade);
            cocktail.HasMany(c => c.Etapes).WithOne().HasForeignKey(e => e.CocktailId).OnDelete(DeleteBehavior.Cascade);
        });

        modele.Entity<ComposantDonnees>(composant =>
        {
            composant.ToTable("CocktailIngredients");
            composant.HasKey(c => new { c.CocktailId, c.IngredientId });
            composant.Property(c => c.Unite).HasMaxLength(20);
            // Un ingrédient utilisé par une recette ne peut pas disparaître du référentiel.
            composant.HasOne(c => c.Ingredient).WithMany().HasForeignKey(c => c.IngredientId).OnDelete(DeleteBehavior.Restrict);
        });

        modele.Entity<EtapeDonnees>(etape =>
        {
            etape.ToTable("EtapesRecette");
            etape.HasKey(e => new { e.CocktailId, e.Ordre });
            etape.Property(e => e.Description).HasMaxLength(2000);
        });

        modele.Entity<BarDonnees>(bar =>
        {
            bar.ToTable("Bars");
            bar.HasKey(b => b.ProprietaireId);
            bar.Property(b => b.Version).IsConcurrencyToken();
            bar.HasMany(b => b.Lignes).WithOne().HasForeignKey(l => l.ProprietaireId).OnDelete(DeleteBehavior.Cascade);
        });

        modele.Entity<LigneStockDonnees>(ligne =>
        {
            ligne.ToTable("LignesStock");
            ligne.HasKey(l => new { l.ProprietaireId, l.IngredientId });
            ligne.Property(l => l.Niveau).HasMaxLength(20);
            ligne.Property(l => l.VolumeUnite).HasMaxLength(5);
            ligne.HasOne(l => l.Ingredient).WithMany().HasForeignKey(l => l.IngredientId).OnDelete(DeleteBehavior.Restrict);
        });

        modele.Entity<NoteDonnees>(note =>
        {
            note.ToTable("Notes");
            note.HasKey(n => new { n.CocktailId, n.UtilisateurId });
            // Supprimer une recette supprime ses notes, dans la même instruction : aucune note orpheline.
            note.HasOne<CocktailDonnees>().WithMany().HasForeignKey(n => n.CocktailId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>Vrai si l'écriture a échoué sur une contrainte d'unicité ou de clé primaire.</summary>
    public static bool EstViolationUnicite(DbUpdateException erreur) =>
        erreur.InnerException is SqliteException { SqliteExtendedErrorCode: SQLITE_CONSTRAINT_UNIQUE or SQLITE_CONSTRAINT_PRIMARYKEY };

    /// <summary>Vrai si l'écriture référence une ligne qui n'existe plus (recette supprimée entre-temps…).</summary>
    public static bool EstViolationCleEtrangere(DbUpdateException erreur) =>
        erreur.InnerException is SqliteException { SqliteExtendedErrorCode: SQLITE_CONSTRAINT_FOREIGNKEY };

    private const int SQLITE_CONSTRAINT_FOREIGNKEY = 787;
    private const int SQLITE_CONSTRAINT_PRIMARYKEY = 1555;
    private const int SQLITE_CONSTRAINT_UNIQUE = 2067;
}
