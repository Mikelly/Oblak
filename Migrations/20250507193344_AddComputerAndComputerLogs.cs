using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AddComputerAndComputerLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ComputerCreatedId",
                table: "MnePersons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Computers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    PCName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Registered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserRegistered = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Logged = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserLogged = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Computers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Computers_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComputerLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Seen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedByUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrowserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OSName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScreenResolution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMobile = table.Column<bool>(type: "bit", nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputerLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComputerLogs_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MnePersons_ComputerCreatedId",
                table: "MnePersons",
                column: "ComputerCreatedId");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerLogs_ComputerId",
                table: "ComputerLogs",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerLogs_Seen",
                table: "ComputerLogs",
                column: "Seen");

            migrationBuilder.CreateIndex(
                name: "IX_Computers_PartnerId",
                table: "Computers",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Computers_PCName",
                table: "Computers",
                column: "PCName");

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_Computers_ComputerCreatedId",
                table: "MnePersons",
                column: "ComputerCreatedId",
                principalTable: "Computers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_Computers_ComputerCreatedId",
                table: "MnePersons");

            migrationBuilder.DropTable(
                name: "ComputerLogs");

            migrationBuilder.DropTable(
                name: "Computers");

            migrationBuilder.DropIndex(
                name: "IX_MnePersons_ComputerCreatedId",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "ComputerCreatedId",
                table: "MnePersons");
        }
    }
}
