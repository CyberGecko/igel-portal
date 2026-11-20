using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IgelPortal.Web.Migrations
{
    /// <inheritdoc />
    public partial class added_biological_sex_to_hedgehogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BiologicalSex",
                table: "Hedgehogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BiologicalSex",
                table: "Hedgehogs");
        }
    }
}
