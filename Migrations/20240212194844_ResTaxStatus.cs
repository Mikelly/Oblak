using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class ResTaxStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResTaxStatus",
                table: "MnePersons",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResTaxAmount",
                table: "Groups",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResTaxStatus",
                table: "Groups",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResTaxStatus",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "ResTaxAmount",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ResTaxStatus",
                table: "Groups");
        }
    }
}
