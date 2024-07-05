using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class added_with_register_on_payment_transactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "PaymentTransactions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<bool>(
                name: "WithRegister",
                table: "PaymentTransactions",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithRegister",
                table: "PaymentTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "PaymentTransactions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
