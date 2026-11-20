using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class changed_lat_lon_to_address : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Territories");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Territories",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Territories",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Territories",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StreetNumber",
                table: "Territories",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Territories");

            migrationBuilder.DropColumn(
                name: "StreetNumber",
                table: "Territories");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Territories",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Territories",
                type: "double",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
