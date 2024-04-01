using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class partners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResidenceTaxAccount",
                table: "Partners",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidenceTaxDescription",
                table: "Partners",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidenceTaxName",
                table: "Partners",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResidenceTaxAccount",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ResidenceTaxDescription",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ResidenceTaxName",
                table: "Partners");
        }
    }
}
