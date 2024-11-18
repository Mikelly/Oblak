using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcursionTaxPrice",
                table: "PartnerTaxSettings");

            migrationBuilder.DropColumn(
                name: "ResTaxFullPrice",
                table: "PartnerTaxSettings");

            migrationBuilder.RenameColumn(
                name: "ResTaxHalfPrice",
                table: "PartnerTaxSettings",
                newName: "TaxPrice");

            migrationBuilder.AddColumn<string>(
                name: "PaymentName",
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
                name: "PaymentName",
                table: "PartnerTaxSettings");

            migrationBuilder.RenameColumn(
                name: "TaxPrice",
                table: "PartnerTaxSettings",
                newName: "ResTaxHalfPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "ExcursionTaxPrice",
                table: "PartnerTaxSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResTaxFullPrice",
                table: "PartnerTaxSettings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
