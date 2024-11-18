using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    CountryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryCode2 = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CountryCode3 = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CountryName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.CountryId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
