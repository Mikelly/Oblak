using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentRef",
                table: "TaxPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Excursions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ExcursionInvoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PartnerTaxSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UseAdvancePayment = table.Column<bool>(type: "bit", nullable: false),
                    PaymentDescription = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PaymentAccount = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PaymentAddress = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerTaxSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerTaxSettings_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerTaxSettings_PartnerId",
                table: "PartnerTaxSettings",
                column: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerTaxSettings");

            migrationBuilder.DropColumn(
                name: "PaymentRef",
                table: "TaxPayments");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Excursions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ExcursionInvoices");
        }
    }
}
