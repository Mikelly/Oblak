using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGroupFKSetNullPT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_Groups_GroupId",
                table: "PaymentTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_Groups_GroupId",
                table: "PaymentTransactions",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransactions_Groups_GroupId",
                table: "PaymentTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransactions_Groups_GroupId",
                table: "PaymentTransactions",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");
        }
    }
}
