using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class ImprovePlacementRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaused",
                table: "TerritoryAttachedServices",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AssignedTerritoryName",
                table: "ServiceRequests",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedTerritoryServiceId",
                table: "ServiceRequests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "AssignedUserName",
                table: "ServiceRequests",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "NeededFrom",
                table: "ServiceRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NeededUntil",
                table: "ServiceRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestNote",
                table: "ServiceRequests",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "ServiceRequestResponses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OfferedTerritoryServiceId",
                table: "ServiceRequestResponses",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_CommunityId_FulfillmentDate",
                table: "ServiceRequests",
                columns: new[] { "CommunityId", "FulfillmentDate" });

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_CommunityId",
                table: "ServiceRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_CommunityId",
                table: "ServiceRequests",
                column: "CommunityId");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_CommunityId_FulfillmentDate",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "IsPaused",
                table: "TerritoryAttachedServices");

            migrationBuilder.DropColumn(
                name: "AssignedTerritoryName",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AssignedTerritoryServiceId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AssignedUserName",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "NeededFrom",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "NeededUntil",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "RequestNote",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "ServiceRequestResponses");

            migrationBuilder.DropColumn(
                name: "OfferedTerritoryServiceId",
                table: "ServiceRequestResponses");

        }
    }
}
