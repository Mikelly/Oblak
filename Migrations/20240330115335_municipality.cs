using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class municipality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Municipality",
                table: "Properties");

            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "Properties",
                type: "int",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Municipality",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Country = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResidenceTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ResidenceTaxAccount = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipality", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_MunicipalityId",
                table: "Properties",
                column: "MunicipalityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Municipality_MunicipalityId",
                table: "Properties",
                column: "MunicipalityId",
                principalTable: "Municipality",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Municipality_MunicipalityId",
                table: "Properties");

            migrationBuilder.DropTable(
                name: "Municipality");

            migrationBuilder.DropIndex(
                name: "IX_Properties_MunicipalityId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Properties");

            migrationBuilder.AddColumn<string>(
                name: "Municipality",
                table: "Properties",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }
    }
}
