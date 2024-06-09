using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class group_restax_calc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_ResTaxTypes_ResTaxTypeId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ResTaxTypeId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ResTaxTypeId",
                table: "Groups");

            migrationBuilder.AddColumn<bool>(
                name: "ResTaxCalculated",
                table: "Groups",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ResTaxPaid",
                table: "Groups",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResTaxCalculated",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ResTaxPaid",
                table: "Groups");

            migrationBuilder.AddColumn<int>(
                name: "ResTaxTypeId",
                table: "Groups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ResTaxTypeId",
                table: "Groups",
                column: "ResTaxTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_ResTaxTypes_ResTaxTypeId",
                table: "Groups",
                column: "ResTaxTypeId",
                principalTable: "ResTaxTypes",
                principalColumn: "Id");
        }
    }
}
