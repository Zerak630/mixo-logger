using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistance.Migrations;

/// <summary>
/// Les comptes passent de la configuration à la base. La table est créée vide : au démarrage, la
/// configuration y crée chaque compte avec l'Id qu'il avait jusque-là (dérivé de son identifiant),
/// si bien que bars, recettes et notes retrouvent leur titulaire.
/// </summary>
public partial class _20260926130000_ComptesEnBase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Comptes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Identifiant = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                IdentifiantNormalise = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                NomAffiche = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                EmpreinteMotDePasse = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                TamponSecurite = table.Column<Guid>(type: "TEXT", nullable: false),
                Actif = table.Column<bool>(type: "INTEGER", nullable: false),
                CreeLe = table.Column<DateTime>(type: "TEXT", nullable: false),
                CleConfiguration = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comptes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Comptes_CleConfiguration",
            table: "Comptes",
            column: "CleConfiguration",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Comptes_IdentifiantNormalise",
            table: "Comptes",
            column: "IdentifiantNormalise",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Comptes");
    }
}
