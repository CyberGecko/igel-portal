using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class added_role_seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "9d263796-50a1-4573-a57c-8afe12834e99", null, "Admin", null },
                    { "e1536d79-28f8-4981-990b-fbeb59878669", null, "User", null },
                    { "e78606b6-1223-42da-9de6-63e2bbfb4d86", null, "CommunityAdmin", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "9d263796-50a1-4573-a57c-8afe12834e99");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e1536d79-28f8-4981-990b-fbeb59878669");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e78606b6-1223-42da-9de6-63e2bbfb4d86");
        }
    }
}
