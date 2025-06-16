using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGroupFKSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_Groups_GroupId",
                table: "MnePersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SrbPersons_Groups_GroupId",
                table: "SrbPersons");

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_Groups_GroupId",
                table: "MnePersons",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SrbPersons_Groups_GroupId",
                table: "SrbPersons",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_Groups_GroupId",
                table: "MnePersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SrbPersons_Groups_GroupId",
                table: "SrbPersons");

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_Groups_GroupId",
                table: "MnePersons",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SrbPersons_Groups_GroupId",
                table: "SrbPersons",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");
        }
    }
}
