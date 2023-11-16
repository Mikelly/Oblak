using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class _19th : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "LegalEntities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceHeader",
                table: "LegalEntities",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Logo",
                table: "LegalEntities",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceHeader",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "LegalEntities");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "LegalEntities",
                type: "int",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);
        }
    }
}
