using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class DefaultPaymentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxPaymentStatus",
                table: "TaxPayments");

            migrationBuilder.AddColumn<int>(
                name: "DefaultPaymentId",
                table: "Partners",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPaymentId",
                table: "Partners");

            migrationBuilder.AddColumn<int>(
                name: "TaxPaymentStatus",
                table: "TaxPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
