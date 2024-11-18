using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Vessels");

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Vessels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vessels_CountryId",
                table: "Vessels",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vessels_Countries_CountryId",
                table: "Vessels",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vessels_Countries_CountryId",
                table: "Vessels");

            migrationBuilder.DropIndex(
                name: "IX_Vessels_CountryId",
                table: "Vessels");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Vessels");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Vessels",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }
    }
}
