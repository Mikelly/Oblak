using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExcursionInvoiceItems_ExcursionInvoices_ExcursionInvoiceId",
                table: "ExcursionInvoiceItems");

            migrationBuilder.RenameColumn(
                name: "Voucher",
                table: "ExcursionInvoiceItems",
                newName: "VoucherNo");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResTaxTypes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResTaxFees",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "ExcursionTaxPrice",
                table: "PartnerTaxSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResTaxFullPrice",
                table: "PartnerTaxSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResTaxHalfPrice",
                table: "PartnerTaxSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "UserModified",
                table: "FiscalRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserCreated",
                table: "FiscalRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ExcursionInvoices",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ExcursionInvoices",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "ExcursionInvoices",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "BillingNote",
                table: "ExcursionInvoices",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcursionInvoiceItems_ExcursionInvoices_ExcursionInvoiceId",
                table: "ExcursionInvoiceItems",
                column: "ExcursionInvoiceId",
                principalTable: "ExcursionInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExcursionInvoiceItems_ExcursionInvoices_ExcursionInvoiceId",
                table: "ExcursionInvoiceItems");

            migrationBuilder.DropColumn(
                name: "ExcursionTaxPrice",
                table: "PartnerTaxSettings");

            migrationBuilder.DropColumn(
                name: "ResTaxFullPrice",
                table: "PartnerTaxSettings");

            migrationBuilder.DropColumn(
                name: "ResTaxHalfPrice",
                table: "PartnerTaxSettings");

            migrationBuilder.RenameColumn(
                name: "VoucherNo",
                table: "ExcursionInvoiceItems",
                newName: "Voucher");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResTaxTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResTaxFees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "UserModified",
                table: "FiscalRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserCreated",
                table: "FiscalRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ExcursionInvoices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "ExcursionInvoices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "ExcursionInvoices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "BillingNote",
                table: "ExcursionInvoices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExcursionInvoiceItems_ExcursionInvoices_ExcursionInvoiceId",
                table: "ExcursionInvoiceItems",
                column: "ExcursionInvoiceId",
                principalTable: "ExcursionInvoices",
                principalColumn: "Id");
        }
    }
}
