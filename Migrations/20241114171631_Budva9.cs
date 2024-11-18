using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class Budva9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(File.ReadAllText("SQL_Countries.txt"));
            migrationBuilder.Sql(File.ReadAllText("SQL_Partner_Budva.txt"));
            migrationBuilder.Sql(File.ReadAllText("SQL_Settings.txt"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
