using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Agencies",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_CountryId",
                table: "Agencies",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agencies_Countries_CountryId",
                table: "Agencies",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agencies_Countries_CountryId",
                table: "Agencies");

            migrationBuilder.DropIndex(
                name: "IX_Agencies_CountryId",
                table: "Agencies");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Agencies");
        }
    }
}
