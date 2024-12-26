using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class ExcurInvCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "ExcursionInvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExcursionInvoiceItems_CountryId",
                table: "ExcursionInvoiceItems",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExcursionInvoiceItems_Countries_CountryId",
                table: "ExcursionInvoiceItems",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExcursionInvoiceItems_Countries_CountryId",
                table: "ExcursionInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_ExcursionInvoiceItems_CountryId",
                table: "ExcursionInvoiceItems");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "ExcursionInvoiceItems");
        }
    }
}
