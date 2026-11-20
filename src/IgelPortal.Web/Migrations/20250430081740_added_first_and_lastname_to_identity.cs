using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class added_first_and_lastname_to_identity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TerritoryAttachedService_Territories_TerritoryId",
                table: "TerritoryAttachedService");

            migrationBuilder.DropForeignKey(
                name: "FK_TerritoryAttachedService_TerritoryServices_TerritoryServiceId",
                table: "TerritoryAttachedService");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TerritoryAttachedService",
                table: "TerritoryAttachedService");

            migrationBuilder.RenameTable(
                name: "TerritoryAttachedService",
                newName: "TerritoryAttachedServices");

            migrationBuilder.RenameIndex(
                name: "IX_TerritoryAttachedService_TerritoryServiceId",
                table: "TerritoryAttachedServices",
                newName: "IX_TerritoryAttachedServices_TerritoryServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_TerritoryAttachedService_TerritoryId",
                table: "TerritoryAttachedServices",
                newName: "IX_TerritoryAttachedServices_TerritoryId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TerritoryAttachedServices",
                table: "TerritoryAttachedServices",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TerritoryAttachedServices_Territories_TerritoryId",
                table: "TerritoryAttachedServices",
                column: "TerritoryId",
                principalTable: "Territories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TerritoryAttachedServices_TerritoryServices_TerritoryService~",
                table: "TerritoryAttachedServices",
                column: "TerritoryServiceId",
                principalTable: "TerritoryServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TerritoryAttachedServices_Territories_TerritoryId",
                table: "TerritoryAttachedServices");

            migrationBuilder.DropForeignKey(
                name: "FK_TerritoryAttachedServices_TerritoryServices_TerritoryService~",
                table: "TerritoryAttachedServices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TerritoryAttachedServices",
                table: "TerritoryAttachedServices");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "TerritoryAttachedServices",
                newName: "TerritoryAttachedService");

            migrationBuilder.RenameIndex(
                name: "IX_TerritoryAttachedServices_TerritoryServiceId",
                table: "TerritoryAttachedService",
                newName: "IX_TerritoryAttachedService_TerritoryServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_TerritoryAttachedServices_TerritoryId",
                table: "TerritoryAttachedService",
                newName: "IX_TerritoryAttachedService_TerritoryId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TerritoryAttachedService",
                table: "TerritoryAttachedService",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TerritoryAttachedService_Territories_TerritoryId",
                table: "TerritoryAttachedService",
                column: "TerritoryId",
                principalTable: "Territories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TerritoryAttachedService_TerritoryServices_TerritoryServiceId",
                table: "TerritoryAttachedService",
                column: "TerritoryServiceId",
                principalTable: "TerritoryServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
