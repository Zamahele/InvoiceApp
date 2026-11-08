using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeInvoiceEmailStatusNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EmailStatus",
                schema: "blacktech",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            // Invoices that were never actually emailed were defaulted to "Pending";
            // clear those so only real send attempts (Sent/Failed) carry a status.
            migrationBuilder.Sql(
                "UPDATE [blacktech].[Invoices] SET [EmailStatus] = NULL WHERE [EmailStatus] = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EmailStatus",
                schema: "blacktech",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
