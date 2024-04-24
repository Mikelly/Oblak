using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class payteon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaytenUserId",
                table: "Properties",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Partners",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaytenUserId",
                table: "LegalEntities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PaytenOrderId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PosTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    LegalEntityId = table.Column<int>(type: "int", nullable: true),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    PaymentSessionToken = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosTransactions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PosTransactions_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosTransactions_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_DocumentId",
                table: "PosTransactions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_LegalEntityId",
                table: "PosTransactions",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTransactions_PropertyId",
                table: "PosTransactions",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosTransactions");

            migrationBuilder.DropColumn(
                name: "PaytenUserId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PaytenUserId",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "PaytenOrderId",
                table: "Documents");

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "Partners",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
