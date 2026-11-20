using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class renamed_territoryserviceterritory_to_territoryattachedservice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerritoryServiceTerritory");

            migrationBuilder.CreateTable(
                name: "TerritoryAttachedService",
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
                    table.PrimaryKey("PK_TerritoryAttachedService", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerritoryAttachedService_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TerritoryAttachedService_TerritoryServices_TerritoryServiceId",
                        column: x => x.TerritoryServiceId,
                        principalTable: "TerritoryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryAttachedService_TerritoryId",
                table: "TerritoryAttachedService",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryAttachedService_TerritoryServiceId",
                table: "TerritoryAttachedService",
                column: "TerritoryServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TerritoryAttachedService");

            migrationBuilder.CreateTable(
                name: "TerritoryServiceTerritory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TerritoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TerritoryServiceId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CurrentCapacity = table.Column<int>(type: "int", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_TerritoryServiceTerritory_TerritoryId",
                table: "TerritoryServiceTerritory",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryServiceTerritory_TerritoryServiceId",
                table: "TerritoryServiceTerritory",
                column: "TerritoryServiceId");
        }
    }
}
