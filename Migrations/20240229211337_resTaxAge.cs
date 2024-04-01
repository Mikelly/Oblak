using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class resTaxAge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgeFrom",
                table: "ResTaxTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgeTo",
                table: "ResTaxTypes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgeFrom",
                table: "ResTaxTypes");

            migrationBuilder.DropColumn(
                name: "AgeTo",
                table: "ResTaxTypes");
        }
    }
}
