using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResTaxTypes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "ResTaxPaymentTypes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResTaxFees",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "NauticalTaxProperty",
                table: "Partners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResidenceTaxDaysLate",
                table: "Partners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseAdvancePayment",
                table: "Partners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Municipalities",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsRegistered",
                table: "LegalEntities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "CheckInPointId",
                table: "Groups",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("UPDATE Groups SET CheckInPointId = NULL");

            migrationBuilder.AddColumn<int>(
                name: "NauticalLegalEntityId",
                table: "Groups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VesselId",
                table: "Groups",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserModified",
                table: "FiscalRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserCreated",
                table: "FiscalRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "DocumentPayments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "VatExempt",
                table: "DocumentItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryCode2 = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CountryCode3 = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CountryName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NauticalTax",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    VesselType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LowerLimitLength = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperLimitLength = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LowerLimitPeriod = table.Column<int>(type: "int", nullable: false),
                    UpperLimitPeriod = table.Column<int>(type: "int", nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NauticalTax", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NauticalTax_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerTaxSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UseAdvancePayment = table.Column<bool>(type: "bit", nullable: false),
                    PaymentDescription = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PaymentAccount = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PaymentAddress = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PaymentName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TaxPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerTaxSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerTaxSettings_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxPaymentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    IsBalanced = table.Column<bool>(type: "bit", nullable: false),
                    IsCash = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxPaymentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxPaymentTypes_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Agencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TIN = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TAX = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PhoneNo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DueDays = table.Column<int>(type: "int", nullable: false),
                    HasContract = table.Column<bool>(type: "bit", nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agencies_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Agencies_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vessels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartnerId = table.Column<int>(type: "int", nullable: false),
                    LegalEntityId = table.Column<int>(type: "int", nullable: true),
                    VesselType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Registration = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    Length = table.Column<int>(type: "int", nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OwnerAddress = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerTIN = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerPhone = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OwnerEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vessels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vessels_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vessels_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vessels_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExcursionInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgencyId = table.Column<int>(type: "int", nullable: false),
                    CheckInPointId = table.Column<int>(type: "int", nullable: false),
                    InvoiceNo = table.Column<int>(type: "int", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillingPeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillingPeriodTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillingNote = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TaxPaymentTypeId = table.Column<int>(type: "int", nullable: false),
                    BillingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BillingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcursionInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcursionInvoices_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExcursionInvoices_CheckInPoints_CheckInPointId",
                        column: x => x.CheckInPointId,
                        principalTable: "CheckInPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExcursionInvoices_TaxPaymentTypes_TaxPaymentTypeId",
                        column: x => x.TaxPaymentTypeId,
                        principalTable: "TaxPaymentTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Excursions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgencyId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    NoOfPersons = table.Column<int>(type: "int", nullable: false),
                    ExcursionTaxExempt = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ExcursionTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Excursions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Excursions_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Excursions_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxPaymentBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: true),
                    AgencyId = table.Column<int>(type: "int", nullable: true),
                    TaxType = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxPaymentBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxPaymentBalances_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxPaymentBalances_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: true),
                    AgencyId = table.Column<int>(type: "int", nullable: true),
                    TaxPaymentTypeId = table.Column<int>(type: "int", nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentRef = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxPayments_Agencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "Agencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxPayments_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaxPayments_TaxPaymentTypes_TaxPaymentTypeId",
                        column: x => x.TaxPaymentTypeId,
                        principalTable: "TaxPaymentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExcursionInvoiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExcursionInvoiceId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxExempt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoOfPersons = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcursionInvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExcursionInvoiceItems_ExcursionInvoices_ExcursionInvoiceId",
                        column: x => x.ExcursionInvoiceId,
                        principalTable: "ExcursionInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CheckInPointId",
                table: "Groups",
                column: "CheckInPointId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_NauticalLegalEntityId",
                table: "Groups",
                column: "NauticalLegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_VesselId",
                table: "Groups",
                column: "VesselId");

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_CountryId",
                table: "Agencies",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_PartnerId",
                table: "Agencies",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcursionInvoiceItems_ExcursionInvoiceId",
                table: "ExcursionInvoiceItems",
                column: "ExcursionInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcursionInvoices_AgencyId",
                table: "ExcursionInvoices",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcursionInvoices_CheckInPointId",
                table: "ExcursionInvoices",
                column: "CheckInPointId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcursionInvoices_TaxPaymentTypeId",
                table: "ExcursionInvoices",
                column: "TaxPaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Excursions_AgencyId",
                table: "Excursions",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Excursions_CountryId",
                table: "Excursions",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_NauticalTax_PartnerId",
                table: "NauticalTax",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerTaxSettings_PartnerId",
                table: "PartnerTaxSettings",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPaymentBalances_AgencyId",
                table: "TaxPaymentBalances",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPaymentBalances_LegalEntityId",
                table: "TaxPaymentBalances",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_AgencyId",
                table: "TaxPayments",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_LegalEntityId",
                table: "TaxPayments",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPayments_TaxPaymentTypeId",
                table: "TaxPayments",
                column: "TaxPaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxPaymentTypes_PartnerId",
                table: "TaxPaymentTypes",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vessels_CountryId",
                table: "Vessels",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Vessels_LegalEntityId",
                table: "Vessels",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Vessels_PartnerId",
                table: "Vessels",
                column: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_CheckInPoints_CheckInPointId",
                table: "Groups",
                column: "CheckInPointId",
                principalTable: "CheckInPoints",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_LegalEntities_NauticalLegalEntityId",
                table: "Groups",
                column: "NauticalLegalEntityId",
                principalTable: "LegalEntities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Vessels_VesselId",
                table: "Groups",
                column: "VesselId",
                principalTable: "Vessels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_CheckInPoints_CheckInPointId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_LegalEntities_NauticalLegalEntityId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Vessels_VesselId",
                table: "Groups");

            migrationBuilder.DropTable(
                name: "ExcursionInvoiceItems");

            migrationBuilder.DropTable(
                name: "Excursions");

            migrationBuilder.DropTable(
                name: "NauticalTax");

            migrationBuilder.DropTable(
                name: "PartnerTaxSettings");

            migrationBuilder.DropTable(
                name: "TaxPaymentBalances");

            migrationBuilder.DropTable(
                name: "TaxPayments");

            migrationBuilder.DropTable(
                name: "Vessels");

            migrationBuilder.DropTable(
                name: "ExcursionInvoices");

            migrationBuilder.DropTable(
                name: "Agencies");

            migrationBuilder.DropTable(
                name: "TaxPaymentTypes");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Groups_CheckInPointId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_NauticalLegalEntityId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_VesselId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "NauticalTaxProperty",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ResidenceTaxDaysLate",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "UseAdvancePayment",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "IsRegistered",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "NauticalLegalEntityId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "VesselId",
                table: "Groups");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResTaxTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "ResTaxPaymentTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResTaxFees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Municipalities",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "CheckInPointId",
                table: "Groups",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserModified",
                table: "FiscalRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserCreated",
                table: "FiscalRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentType",
                table: "DocumentPayments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "VatExempt",
                table: "DocumentItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
