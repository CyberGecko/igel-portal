using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class changed_hedgehog_territories_added_territory_services : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hedgehogs_Territories_TerritoryId",
                table: "Hedgehogs");

            migrationBuilder.RenameColumn(
                name: "TerritoryId",
                table: "Hedgehogs",
                newName: "HomeTerritoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Hedgehogs_TerritoryId",
                table: "Hedgehogs",
                newName: "IX_Hedgehogs_HomeTerritoryId");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentTerritoryId",
                table: "Hedgehogs",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "TerritoryServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerritoryServices", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TerritoryServiceTerritory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TerritoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TerritoryServiceId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentCapacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerritoryServiceTerritory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerritoryServiceTerritory_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TerritoryServiceTerritory_TerritoryServices_TerritoryService~",
                        column: x => x.TerritoryServiceId,
                        principalTable: "TerritoryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Hedgehogs_CurrentTerritoryId",
                table: "Hedgehogs",
                column: "CurrentTerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryServiceTerritory_TerritoryId",
                table: "TerritoryServiceTerritory",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryServiceTerritory_TerritoryServiceId",
                table: "TerritoryServiceTerritory",
                column: "TerritoryServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hedgehogs_Territories_CurrentTerritoryId",
                table: "Hedgehogs",
                column: "CurrentTerritoryId",
                principalTable: "Territories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Hedgehogs_Territories_HomeTerritoryId",
                table: "Hedgehogs",
                column: "HomeTerritoryId",
                principalTable: "Territories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hedgehogs_Territories_CurrentTerritoryId",
                table: "Hedgehogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Hedgehogs_Territories_HomeTerritoryId",
                table: "Hedgehogs");

            migrationBuilder.DropTable(
                name: "TerritoryServiceTerritory");

            migrationBuilder.DropTable(
                name: "TerritoryServices");

            migrationBuilder.DropIndex(
                name: "IX_Hedgehogs_CurrentTerritoryId",
                table: "Hedgehogs");

            migrationBuilder.DropColumn(
                name: "CurrentTerritoryId",
                table: "Hedgehogs");

            migrationBuilder.RenameColumn(
                name: "HomeTerritoryId",
                table: "Hedgehogs",
                newName: "TerritoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Hedgehogs_HomeTerritoryId",
                table: "Hedgehogs",
                newName: "IX_Hedgehogs_TerritoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hedgehogs_Territories_TerritoryId",
                table: "Hedgehogs",
                column: "TerritoryId",
                principalTable: "Territories",
                principalColumn: "Id");
        }
    }
}
