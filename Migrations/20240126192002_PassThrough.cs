using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class PassThrough : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PassThroughId",
                table: "LegalEntities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_PassThroughId",
                table: "LegalEntities",
                column: "PassThroughId");

            migrationBuilder.AddForeignKey(
                name: "FK_LegalEntities_LegalEntities_PassThroughId",
                table: "LegalEntities",
                column: "PassThroughId",
                principalTable: "LegalEntities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LegalEntities_LegalEntities_PassThroughId",
                table: "LegalEntities");

            migrationBuilder.DropIndex(
                name: "IX_LegalEntities_PassThroughId",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "PassThroughId",
                table: "LegalEntities");
        }
    }
}
