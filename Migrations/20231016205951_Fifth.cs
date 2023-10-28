using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Fifth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Change",
                table: "SrbPersons",
                newName: "CheckedOut");

            migrationBuilder.AddColumn<bool>(
                name: "CheckedIn",
                table: "SrbPersons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckedIn",
                table: "SrbPersons");

            migrationBuilder.RenameColumn(
                name: "CheckedOut",
                table: "SrbPersons",
                newName: "Change");
        }
    }
}
