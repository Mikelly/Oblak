using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePersonResTaxHistoryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResTaxHistory_MnePersons_PersonId",
                table: "ResTaxHistory");

            migrationBuilder.AddForeignKey(
                name: "FK_ResTaxHistory_MnePersons_PersonId",
                table: "ResTaxHistory",
                column: "PersonId",
                principalTable: "MnePersons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResTaxHistory_MnePersons_PersonId",
                table: "ResTaxHistory");

            migrationBuilder.AddForeignKey(
                name: "FK_ResTaxHistory_MnePersons_PersonId",
                table: "ResTaxHistory",
                column: "PersonId",
                principalTable: "MnePersons",
                principalColumn: "Id");
        }
    }
}
