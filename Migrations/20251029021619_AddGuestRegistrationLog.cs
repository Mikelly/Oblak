using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestRegistrationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuestRegistrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MnePersonId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<int>(type: "int", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    InitializedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ValidationErrors = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExternalErrors = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestRegistrationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestRegistrationLogs_MnePersons_MnePersonId",
                        column: x => x.MnePersonId,
                        principalTable: "MnePersons",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestRegistrationLogs_MnePersonId",
                table: "GuestRegistrationLogs",
                column: "MnePersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestRegistrationLogs");
        }
    }
}
