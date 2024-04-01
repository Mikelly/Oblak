using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class checkinpoints2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonName",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInPointId",
                table: "SrbPersons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInPointId",
                table: "MnePersons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SrbPersons_CheckInPointId",
                table: "SrbPersons",
                column: "CheckInPointId");

            migrationBuilder.CreateIndex(
                name: "IX_MnePersons_CheckInPointId",
                table: "MnePersons",
                column: "CheckInPointId");

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_CheckInPoints_CheckInPointId",
                table: "MnePersons",
                column: "CheckInPointId",
                principalTable: "CheckInPoints",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SrbPersons_CheckInPoints_CheckInPointId",
                table: "SrbPersons",
                column: "CheckInPointId",
                principalTable: "CheckInPoints",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_CheckInPoints_CheckInPointId",
                table: "MnePersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SrbPersons_CheckInPoints_CheckInPointId",
                table: "SrbPersons");

            migrationBuilder.DropIndex(
                name: "IX_SrbPersons_CheckInPointId",
                table: "SrbPersons");

            migrationBuilder.DropIndex(
                name: "IX_MnePersons_CheckInPointId",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "PersonName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CheckInPointId",
                table: "SrbPersons");

            migrationBuilder.DropColumn(
                name: "CheckInPointId",
                table: "MnePersons");
        }
    }
}
