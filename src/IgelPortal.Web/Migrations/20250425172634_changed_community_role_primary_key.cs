using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class changed_community_role_primary_key : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CommunityMembers",
                table: "CommunityMembers");

            migrationBuilder.AlterColumn<string>(
                name: "CommunityRoleName",
                table: "CommunityMembers",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommunityMembers",
                table: "CommunityMembers",
                columns: new[] { "UserName", "CommunityId", "CommunityRoleName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CommunityMembers",
                table: "CommunityMembers");

            migrationBuilder.AlterColumn<string>(
                name: "CommunityRoleName",
                table: "CommunityMembers",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommunityMembers",
                table: "CommunityMembers",
                columns: new[] { "UserName", "CommunityId" });
        }
    }
}
