using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Excursions");

            migrationBuilder.RenameColumn(
                name: "CountryId",
                table: "Countries",
                newName: "Id");

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Excursions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Excursions_CountryId",
                table: "Excursions",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Excursions_Countries_CountryId",
                table: "Excursions",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Excursions_Countries_CountryId",
                table: "Excursions");

            migrationBuilder.DropIndex(
                name: "IX_Excursions_CountryId",
                table: "Excursions");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Excursions");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Countries",
                newName: "CountryId");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Excursions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }
    }
}
