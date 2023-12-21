using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "PersonSequence");

            migrationBuilder.CreateTable(
                name: "CodeLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Country = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Param1 = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Param2 = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Param3 = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Base64Data = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Country = table.Column<int>(type: "int", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TIN = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalEntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TIN = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InvoiceHeader = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Logo = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    InVat = table.Column<bool>(type: "bit", nullable: false),
                    EfiCertData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    EfiPassword = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Rb90CertData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Rb90Password = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SrbRbUserName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SrbRbPassword = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SrbRbToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SrbRbRefreshToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalEntities_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiscalEnu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    No = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AutoDeposit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayteonId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayteonPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayteonUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalEnu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalEnu_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiscalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    BusinessUnitCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FiscalEnuCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FicalizationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Invoice = table.Column<int>(type: "int", nullable: false),
                    InvoiceNo = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<int>(type: "int", maxLength: 450, nullable: false),
                    Request = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TCR = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IIC = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FIC = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FCDC = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalRequests_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PriceInclVat = table.Column<bool>(type: "bit", nullable: false),
                    VatExempt = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalId = table.Column<int>(type: "int", nullable: false),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    RegNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RegDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Place = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Municipality = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    GeoLon = table.Column<decimal>(type: "decimal(12,8)", precision: 12, scale: 8, nullable: true),
                    GeoLat = table.Column<decimal>(type: "decimal(12,8)", precision: 12, scale: 8, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResidenceTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ResidenceTaxYN = table.Column<bool>(type: "bit", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BusinessUnitCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FiscalEnuCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DefaultEntryPoint = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AutoCheckOut = table.Column<bool>(type: "bit", nullable: false),
                    AutoCheckOutTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PropertyUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FloorNo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RoomNo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(12,8)", precision: 12, scale: 8, nullable: false),
                    Naplata = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyUnits_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LegalEntityId = table.Column<int>(type: "int", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EfiOperator = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CheckInPointId = table.Column<int>(type: "int", nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    CheckInPointId = table.Column<int>(type: "int", nullable: false),
                    PropertyExternalId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    Guid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Groups_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResTaxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    PropertyRefId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                name: "SelfRegisterTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    Guid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PhoneNo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: true),
                    PropertyUnitId = table.Column<int>(type: "int", nullable: true),
                    Sent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfRegisterTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfRegisterTokens_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SelfRegisterTokens_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IdEncrypted = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    InvoiceType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TypeOfInvoce = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BusinessUnitCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FiscalEnuCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FiscalizationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    No = table.Column<int>(type: "int", nullable: false),
                    OrdinalNo = table.Column<int>(type: "int", nullable: false),
                    ExternalNo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FiscalNo = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IIC = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FIC = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    OperatorCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    PartnerName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PartnerType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PartnerIdType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PartnerIdNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PartnerAddress = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Qr = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    QrPath = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MnePersons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [PersonSequence]"),
                    Guid = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ExternalId = table.Column<int>(type: "int", nullable: true),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    PropertyExternalId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PersonalNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", maxLength: 450, nullable: true),
                    BirthPlace = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BirthCountry = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PersonType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PermanentResidenceCountry = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PermanentResidencePlace = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PermanentResidenceAddress = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DocumentValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DocumentCountry = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DocumentIssuer = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    VisaType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VisaNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VisaValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VisaValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VisaIssuePlace = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EntryPoint = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EntryPointDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Other = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResTaxType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MnePersons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MnePersons_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MnePersons_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MnePersons_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SrbPersons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [PersonSequence]"),
                    Guid = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ExternalId = table.Column<int>(type: "int", nullable: true),
                    LegalEntityId = table.Column<int>(type: "int", nullable: false),
                    PropertyExternalId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PersonalNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UserCreated = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserCreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserModified = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserModifiedDate = table.Column<DateTime>(type: "datetime2", maxLength: 450, nullable: true),
                    ExternalId2 = table.Column<int>(type: "int", nullable: true),
                    IsDomestic = table.Column<bool>(type: "bit", nullable: false),
                    IsForeignBorn = table.Column<bool>(type: "bit", nullable: false),
                    BirthPlaceName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BirthCountryIso2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    BirthCountryIso3 = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    NationalityIso2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    NationalityIso3 = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ResidenceCountryIso2 = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    ResidenceCountryIso3 = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    ResidenceMunicipalityCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResidenceMunicipalityName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResidencePlaceCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResidencePlaceName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DocumentNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DocumentIssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VisaType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VisaNumber = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    VisaIssuingPlace = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntryPlaceCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EntryPlace = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StayValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuingAuthorithy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ArrivalType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Vouchers = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CheckIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedIn = table.Column<bool>(type: "bit", nullable: false),
                    PlannedCheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResidenceTaxDiscountReason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReasonForStay = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    NumberOfServices = table.Column<int>(type: "int", nullable: true),
                    CheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedOut = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SrbPersons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SrbPersons_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SrbPersons_LegalEntities_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SrbPersons_Properties_PropertyId",
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
                    TaxType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GuestType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    NumberOfGuests = table.Column<int>(type: "int", nullable: false),
                    NumberOfNights = table.Column<int>(type: "int", nullable: false),
                    TaxPerNight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "DocumentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ItemUnit = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    VatExempt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPriceWoVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FinalPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTotalWoVat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentItems_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentPayments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentItems_DocumentId",
                table: "DocumentItems",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentItems_ItemId",
                table: "DocumentItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPayments_DocumentId",
                table: "DocumentPayments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_GroupId",
                table: "Documents",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_LegalEntityId",
                table: "Documents",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PropertyId",
                table: "Documents",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalEnu_LegalEntityId",
                table: "FiscalEnu",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalRequests_LegalEntityId",
                table: "FiscalRequests",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_LegalEntityId",
                table: "Groups",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PropertyId",
                table: "Groups",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_LegalEntityId",
                table: "Items",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_PartnerId",
                table: "LegalEntities",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MnePersons_GroupId",
                table: "MnePersons",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MnePersons_LegalEntityId",
                table: "MnePersons",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MnePersons_PropertyId",
                table: "MnePersons",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_LegalEntityId",
                table: "Properties",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyUnits_LegalEntityId",
                table: "PropertyUnits",
                column: "LegalEntityId");

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

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SelfRegisterTokens_LegalEntityId",
                table: "SelfRegisterTokens",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfRegisterTokens_PropertyId",
                table: "SelfRegisterTokens",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SrbPersons_GroupId",
                table: "SrbPersons",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SrbPersons_LegalEntityId",
                table: "SrbPersons",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SrbPersons_PropertyId",
                table: "SrbPersons",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LegalEntityId",
                table: "Users",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeLists");

            migrationBuilder.DropTable(
                name: "DocumentItems");

            migrationBuilder.DropTable(
                name: "DocumentPayments");

            migrationBuilder.DropTable(
                name: "FiscalEnu");

            migrationBuilder.DropTable(
                name: "FiscalRequests");

            migrationBuilder.DropTable(
                name: "MnePersons");

            migrationBuilder.DropTable(
                name: "PropertyUnits");

            migrationBuilder.DropTable(
                name: "ResTaxItems");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "SelfRegisterTokens");

            migrationBuilder.DropTable(
                name: "SrbPersons");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "ResTaxes");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "LegalEntities");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropSequence(
                name: "PersonSequence");
        }
    }
}
