using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class ResTaxHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResTaxHistory_MnePersons_PersonId",
                table: "ResTaxHistory");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "ResTaxHistory",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PrevResTaxTypeId",
                table: "ResTaxHistory",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ResTaxHistory_MnePersons_PersonId",
                table: "ResTaxHistory",
                column: "PersonId",
                principalTable: "MnePersons",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResTaxHistory_MnePersons_PersonId",
                table: "ResTaxHistory");

            migrationBuilder.DropColumn(
                name: "PrevResTaxTypeId",
                table: "ResTaxHistory");

            migrationBuilder.AlterColumn<int>(
                name: "PersonId",
                table: "ResTaxHistory",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ResTaxHistory_MnePersons_PersonId",
                table: "ResTaxHistory",
                column: "PersonId",
                principalTable: "MnePersons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
