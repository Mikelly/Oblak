using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class RenameGuestRegistrationLogToGuestAutoRegistrationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestRegistrationLogs");

            migrationBuilder.CreateTable(
                name: "GuestAutoRegistrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MnePersonId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<int>(type: "int", nullable: true),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    CheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    InitializedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ValidationErrors = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExternalErrors = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestAutoRegistrationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestAutoRegistrationLogs_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GuestAutoRegistrationLogs_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GuestAutoRegistrationLogs_MnePersons_MnePersonId",
                        column: x => x.MnePersonId,
                        principalTable: "MnePersons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GuestAutoRegistrationLogs_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuestAutoRegistrationLogs_GroupId",
                table: "GuestAutoRegistrationLogs",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestAutoRegistrationLogs_LegalEntityId",
                table: "GuestAutoRegistrationLogs",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestAutoRegistrationLogs_MnePersonId",
                table: "GuestAutoRegistrationLogs",
                column: "MnePersonId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestAutoRegistrationLogs_PropertyId",
                table: "GuestAutoRegistrationLogs",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuestAutoRegistrationLogs");

            migrationBuilder.CreateTable(
                name: "GuestRegistrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MnePersonId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExternalErrors = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExternalId = table.Column<int>(type: "int", nullable: true),
                    InitializedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ValidationErrors = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
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
    }
}
