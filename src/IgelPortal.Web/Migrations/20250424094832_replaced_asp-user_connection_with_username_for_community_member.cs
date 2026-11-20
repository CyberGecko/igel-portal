using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class replaced_aspuser_connection_with_username_for_community_member : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunityMembers_AspNetUsers_UserId",
                table: "CommunityMembers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "CommunityMembers",
                newName: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "CommunityMembers",
                newName: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunityMembers_AspNetUsers_UserId",
                table: "CommunityMembers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
