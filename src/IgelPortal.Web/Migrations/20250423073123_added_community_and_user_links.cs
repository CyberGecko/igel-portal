using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class added_community_and_user_links : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CommunityId",
                table: "Territories",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Territories",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AttachedUser",
                table: "Hedgehogs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "CommunityId",
                table: "Hedgehogs",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "Communities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactMail = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactPhone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangeDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Territories_CommunityId",
                table: "Territories",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Hedgehogs_CommunityId",
                table: "Hedgehogs",
                column: "CommunityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hedgehogs_Communities_CommunityId",
                table: "Hedgehogs",
                column: "CommunityId",
                principalTable: "Communities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Territories_Communities_CommunityId",
                table: "Territories",
                column: "CommunityId",
                principalTable: "Communities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hedgehogs_Communities_CommunityId",
                table: "Hedgehogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Territories_Communities_CommunityId",
                table: "Territories");

            migrationBuilder.DropTable(
                name: "Communities");

            migrationBuilder.DropIndex(
                name: "IX_Territories_CommunityId",
                table: "Territories");

            migrationBuilder.DropIndex(
                name: "IX_Hedgehogs_CommunityId",
                table: "Hedgehogs");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "58dd405a-db88-41ed-bcf8-2b605c8de222");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5ff84f3c-5963-4b7b-932b-2faf489a23fb");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a2259844-ce5d-4b79-b5c4-faff862d9720");

            migrationBuilder.DropColumn(
                name: "CommunityId",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "AttachedUser",
                table: "Hedgehogs");

            migrationBuilder.DropColumn(
                name: "CommunityId",
                table: "Hedgehogs");
        }
    }
}
