using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class ResTaxHistoryAndExempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResTaxExemptionTypeId",
                table: "MnePersons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResTaxExemptionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxExemptionTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResTaxExemptionTypes_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResTaxHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    PrevCheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrevCheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrevResTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PrevResFeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PrevResTaxPaymentTypeId = table.Column<int>(type: "int", nullable: true),
                    PrevResTaxExemptionTypeId = table.Column<int>(type: "int", nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResTaxHistory_MnePersons_PersonId",
                        column: x => x.PersonId,
                        principalTable: "MnePersons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MnePersons_ResTaxExemptionTypeId",
                table: "MnePersons",
                column: "ResTaxExemptionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxExemptionTypes_PartnerId",
                table: "ResTaxExemptionTypes",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxHistory_PersonId",
                table: "ResTaxHistory",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_ResTaxExemptionTypes_ResTaxExemptionTypeId",
                table: "MnePersons",
                column: "ResTaxExemptionTypeId",
                principalTable: "ResTaxExemptionTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_ResTaxExemptionTypes_ResTaxExemptionTypeId",
                table: "MnePersons");

            migrationBuilder.DropTable(
                name: "ResTaxExemptionTypes");

            migrationBuilder.DropTable(
                name: "ResTaxHistory");

            migrationBuilder.DropIndex(
                name: "IX_MnePersons_ResTaxExemptionTypeId",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "ResTaxExemptionTypeId",
                table: "MnePersons");
        }
    }
}
