using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class added_reference_between_attached_service_and_hedgehogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCapacity",
                table: "TerritoryAttachedServices");

            migrationBuilder.AddColumn<Guid>(
                name: "UsedServiceId",
                table: "Hedgehogs",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Hedgehogs_UsedServiceId",
                table: "Hedgehogs",
                column: "UsedServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hedgehogs_TerritoryAttachedServices_UsedServiceId",
                table: "Hedgehogs",
                column: "UsedServiceId",
                principalTable: "TerritoryAttachedServices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hedgehogs_TerritoryAttachedServices_UsedServiceId",
                table: "Hedgehogs");

            migrationBuilder.DropIndex(
                name: "IX_Hedgehogs_UsedServiceId",
                table: "Hedgehogs");

            migrationBuilder.DropColumn(
                name: "UsedServiceId",
                table: "Hedgehogs");

            migrationBuilder.AddColumn<int>(
                name: "CurrentCapacity",
                table: "TerritoryAttachedServices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
