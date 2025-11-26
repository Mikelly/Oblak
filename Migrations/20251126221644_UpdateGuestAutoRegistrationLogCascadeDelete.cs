using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGuestAutoRegistrationLogCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestAutoRegistrationLogs_MnePersons_MnePersonId",
                table: "GuestAutoRegistrationLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestAutoRegistrationLogs_MnePersons_MnePersonId",
                table: "GuestAutoRegistrationLogs",
                column: "MnePersonId",
                principalTable: "MnePersons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GuestAutoRegistrationLogs_MnePersons_MnePersonId",
                table: "GuestAutoRegistrationLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_GuestAutoRegistrationLogs_MnePersons_MnePersonId",
                table: "GuestAutoRegistrationLogs",
                column: "MnePersonId",
                principalTable: "MnePersons",
                principalColumn: "Id");
        }
    }
}
