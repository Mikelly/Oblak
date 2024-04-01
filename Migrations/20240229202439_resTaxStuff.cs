using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class resTaxStuff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResTaxItems");

            migrationBuilder.DropTable(
                name: "ResTaxes");

            migrationBuilder.DropColumn(
                name: "ResTaxStatus",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "ResTaxType",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "ResTaxStatus",
                table: "Groups");

            migrationBuilder.AddColumn<decimal>(
                name: "ResTaxFee",
                table: "MnePersons",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResTaxPaymentTypeId",
                table: "MnePersons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResTaxTypeId",
                table: "MnePersons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResTaxFee",
                table: "Groups",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResTaxPaymentTypeId",
                table: "Groups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResTaxTypeId",
                table: "Groups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResTaxCalc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxCalc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResTaxCalc_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResTaxCalc_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResTaxPaymentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxPaymentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResTaxPaymentTypes_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResTaxTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResTaxTypes_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResTaxCalcItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResTaxID = table.Column<int>(type: "int", nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GuestType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    NumberOfGuests = table.Column<int>(type: "int", nullable: false),
                    NumberOfNights = table.Column<int>(type: "int", nullable: false),
                    TaxPerNight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxCalcItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ResTaxCalcItems_ResTaxCalc_ResTaxID",
                        column: x => x.ResTaxID,
                        principalTable: "ResTaxCalc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResTaxFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResTaxPaymentStatusId = table.Column<int>(type: "int", nullable: false),
                    ResTaxPaymentTypeId = table.Column<int>(type: "int", nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FeePercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LowerLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResTaxFees_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResTaxFees_ResTaxPaymentTypes_ResTaxPaymentTypeId",
                        column: x => x.ResTaxPaymentTypeId,
                        principalTable: "ResTaxPaymentTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MnePersons_ResTaxPaymentTypeId",
                table: "MnePersons",
                column: "ResTaxPaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MnePersons_ResTaxTypeId",
                table: "MnePersons",
                column: "ResTaxTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ResTaxPaymentTypeId",
                table: "Groups",
                column: "ResTaxPaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ResTaxTypeId",
                table: "Groups",
                column: "ResTaxTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxCalc_LegalEntityId",
                table: "ResTaxCalc",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxCalc_PropertyId",
                table: "ResTaxCalc",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxCalcItems_ResTaxID",
                table: "ResTaxCalcItems",
                column: "ResTaxID");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxFees_PartnerId",
                table: "ResTaxFees",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxFees_ResTaxPaymentTypeId",
                table: "ResTaxFees",
                column: "ResTaxPaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxPaymentTypes_PartnerId",
                table: "ResTaxPaymentTypes",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxTypes_PartnerId",
                table: "ResTaxTypes",
                column: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_ResTaxPaymentTypes_ResTaxPaymentTypeId",
                table: "Groups",
                column: "ResTaxPaymentTypeId",
                principalTable: "ResTaxPaymentTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_ResTaxTypes_ResTaxTypeId",
                table: "Groups",
                column: "ResTaxTypeId",
                principalTable: "ResTaxTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_ResTaxPaymentTypes_ResTaxPaymentTypeId",
                table: "MnePersons",
                column: "ResTaxPaymentTypeId",
                principalTable: "ResTaxPaymentTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_ResTaxTypes_ResTaxTypeId",
                table: "MnePersons",
                column: "ResTaxTypeId",
                principalTable: "ResTaxTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_ResTaxPaymentTypes_ResTaxPaymentTypeId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_ResTaxTypes_ResTaxTypeId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_ResTaxPaymentTypes_ResTaxPaymentTypeId",
                table: "MnePersons");

            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_ResTaxTypes_ResTaxTypeId",
                table: "MnePersons");

            migrationBuilder.DropTable(
                name: "ResTaxCalcItems");

            migrationBuilder.DropTable(
                name: "ResTaxFees");

            migrationBuilder.DropTable(
                name: "ResTaxTypes");

            migrationBuilder.DropTable(
                name: "ResTaxCalc");

            migrationBuilder.DropTable(
                name: "ResTaxPaymentTypes");

            migrationBuilder.DropIndex(
                name: "IX_MnePersons_ResTaxPaymentTypeId",
                table: "MnePersons");

            migrationBuilder.DropIndex(
                name: "IX_MnePersons_ResTaxTypeId",
                table: "MnePersons");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ResTaxPaymentTypeId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ResTaxTypeId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ResTaxFee",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "ResTaxPaymentTypeId",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "ResTaxTypeId",
                table: "MnePersons");

            migrationBuilder.DropColumn(
                name: "ResTaxFee",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ResTaxPaymentTypeId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ResTaxTypeId",
                table: "Groups");

            migrationBuilder.AddColumn<string>(
                name: "ResTaxStatus",
                table: "MnePersons",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResTaxType",
                table: "MnePersons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResTaxStatus",
                table: "Groups",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PropertyRefId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResTaxes_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResTaxes_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResTaxItems",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResTaxID = table.Column<int>(type: "int", nullable: false),
                    GuestType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    NumberOfGuests = table.Column<int>(type: "int", nullable: false),
                    NumberOfNights = table.Column<int>(type: "int", nullable: false),
                    TaxPerNight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResTaxItems", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ResTaxItems_ResTaxes_ResTaxID",
                        column: x => x.ResTaxID,
                        principalTable: "ResTaxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxes_LegalEntityId",
                table: "ResTaxes",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxes_PropertyId",
                table: "ResTaxes",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_ResTaxItems_ResTaxID",
                table: "ResTaxItems",
                column: "ResTaxID");
        }
    }
}
