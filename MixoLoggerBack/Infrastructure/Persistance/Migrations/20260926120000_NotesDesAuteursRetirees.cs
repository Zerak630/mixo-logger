using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistance.Migrations;

/// <summary>
/// Données seulement, schéma inchangé : l'auteur ne peut plus noter sa propre recette. Les notes
/// qu'il y avait déjà mises sont retirées, pour que les moyennes affichées suivent la règle.
/// </summary>
public partial class _20260926120000_NotesDesAuteursRetirees : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "Notes"
            WHERE "UtilisateurId" = (SELECT "AuteurId" FROM "Cocktails" WHERE "Cocktails"."Id" = "Notes"."CocktailId");
            """);
    }

    /// <inheritdoc />
    /// <remarks>Les notes retirées ne peuvent pas être restaurées : rien à défaire.</remarks>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
