using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "PartnerTaxSettings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "PartnerTaxSettings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "PartnerTaxSettings");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "PartnerTaxSettings");
        }
    }
}
