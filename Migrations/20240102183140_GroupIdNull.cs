using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class GroupIdNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_Groups_GroupId",
                table: "MnePersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SrbPersons_Groups_GroupId",
                table: "SrbPersons");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "SrbPersons",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "MnePersons",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AdministratorId",
                table: "LegalEntities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalEntities_AdministratorId",
                table: "LegalEntities",
                column: "AdministratorId");

            migrationBuilder.AddForeignKey(
                name: "FK_LegalEntities_LegalEntities_AdministratorId",
                table: "LegalEntities",
                column: "AdministratorId",
                principalTable: "LegalEntities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_Groups_GroupId",
                table: "MnePersons",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SrbPersons_Groups_GroupId",
                table: "SrbPersons",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LegalEntities_LegalEntities_AdministratorId",
                table: "LegalEntities");

            migrationBuilder.DropForeignKey(
                name: "FK_MnePersons_Groups_GroupId",
                table: "MnePersons");

            migrationBuilder.DropForeignKey(
                name: "FK_SrbPersons_Groups_GroupId",
                table: "SrbPersons");

            migrationBuilder.DropIndex(
                name: "IX_LegalEntities_AdministratorId",
                table: "LegalEntities");

            migrationBuilder.DropColumn(
                name: "AdministratorId",
                table: "LegalEntities");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "SrbPersons",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "MnePersons",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MnePersons_Groups_GroupId",
                table: "MnePersons",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SrbPersons_Groups_GroupId",
                table: "SrbPersons",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
