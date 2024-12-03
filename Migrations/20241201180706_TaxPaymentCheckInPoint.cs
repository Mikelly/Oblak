using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class TaxPaymentCheckInPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CheckInPointId",
                table: "TaxPayments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_CheckInPointId",
                table: "TaxPayments",
                column: "CheckInPointId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxPayments_CheckInPoints_CheckInPointId",
                table: "TaxPayments",
                column: "CheckInPointId",
                principalTable: "CheckInPoints",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxPayments_CheckInPoints_CheckInPointId",
                table: "TaxPayments");

            migrationBuilder.DropIndex(
                name: "IX_TaxPayments_CheckInPointId",
                table: "TaxPayments");

            migrationBuilder.DropColumn(
                name: "CheckInPointId",
                table: "TaxPayments");
        }
    }
}
