using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistance.Migrations;

/// <inheritdoc />
public partial class _20260915113525_Initiale : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Bars",
            columns: table => new
            {
                ProprietaireId = table.Column<Guid>(type: "TEXT", nullable: false),
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CreeLe = table.Column<DateTime>(type: "TEXT", nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bars", x => x.ProprietaireId);
            });

        migrationBuilder.CreateTable(
            name: "Cocktails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                NomNormalise = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                AuteurId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cocktails", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Ingredients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Nom = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                NomNormalise = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreeLe = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ingredients", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EtapesRecette",
            columns: table => new
            {
                CocktailId = table.Column<Guid>(type: "TEXT", nullable: false),
                Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EtapesRecette", x => new { x.CocktailId, x.Ordre });
                table.ForeignKey(
                    name: "FK_EtapesRecette_Cocktails_CocktailId",
                    column: x => x.CocktailId,
                    principalTable: "Cocktails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notes",
            columns: table => new
            {
                CocktailId = table.Column<Guid>(type: "TEXT", nullable: false),
                UtilisateurId = table.Column<Guid>(type: "TEXT", nullable: false),
                Valeur = table.Column<int>(type: "INTEGER", nullable: false),
                NoteeLe = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notes", x => new { x.CocktailId, x.UtilisateurId });
                table.ForeignKey(
                    name: "FK_Notes_Cocktails_CocktailId",
                    column: x => x.CocktailId,
                    principalTable: "Cocktails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AliasIngredients",
            columns: table => new
            {
                Alias = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                IngredientId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AliasIngredients", x => x.Alias);
                table.ForeignKey(
                    name: "FK_AliasIngredients_Ingredients_IngredientId",
                    column: x => x.IngredientId,
                    principalTable: "Ingredients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CocktailIngredients",
            columns: table => new
            {
                CocktailId = table.Column<Guid>(type: "TEXT", nullable: false),
                IngredientId = table.Column<Guid>(type: "TEXT", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Valeur = table.Column<double>(type: "REAL", nullable: false),
                Unite = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CocktailIngredients", x => new { x.CocktailId, x.IngredientId });
                table.ForeignKey(
                    name: "FK_CocktailIngredients_Cocktails_CocktailId",
                    column: x => x.CocktailId,
                    principalTable: "Cocktails",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CocktailIngredients_Ingredients_IngredientId",
                    column: x => x.IngredientId,
                    principalTable: "Ingredients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LignesStock",
            columns: table => new
            {
                ProprietaireId = table.Column<Guid>(type: "TEXT", nullable: false),
                IngredientId = table.Column<Guid>(type: "TEXT", nullable: false),
                Niveau = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                VolumeValeur = table.Column<double>(type: "REAL", nullable: true),
                VolumeUnite = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LignesStock", x => new { x.ProprietaireId, x.IngredientId });
                table.ForeignKey(
                    name: "FK_LignesStock_Bars_ProprietaireId",
                    column: x => x.ProprietaireId,
                    principalTable: "Bars",
                    principalColumn: "ProprietaireId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LignesStock_Ingredients_IngredientId",
                    column: x => x.IngredientId,
                    principalTable: "Ingredients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AliasIngredients_IngredientId",
            table: "AliasIngredients",
            column: "IngredientId");

        migrationBuilder.CreateIndex(
            name: "IX_CocktailIngredients_IngredientId",
            table: "CocktailIngredients",
            column: "IngredientId");

        migrationBuilder.CreateIndex(
            name: "IX_Cocktails_AuteurId",
            table: "Cocktails",
            column: "AuteurId");

        migrationBuilder.CreateIndex(
            name: "IX_Cocktails_NomNormalise",
            table: "Cocktails",
            column: "NomNormalise",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Ingredients_NomNormalise",
            table: "Ingredients",
            column: "NomNormalise",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LignesStock_IngredientId",
            table: "LignesStock",
            column: "IngredientId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AliasIngredients");

        migrationBuilder.DropTable(
            name: "CocktailIngredients");

        migrationBuilder.DropTable(
            name: "EtapesRecette");

        migrationBuilder.DropTable(
            name: "LignesStock");

        migrationBuilder.DropTable(
            name: "Notes");

        migrationBuilder.DropTable(
            name: "Bars");

        migrationBuilder.DropTable(
            name: "Ingredients");

        migrationBuilder.DropTable(
            name: "Cocktails");
    }
}
