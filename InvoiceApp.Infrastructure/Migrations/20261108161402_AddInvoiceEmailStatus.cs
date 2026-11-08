using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceEmailStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailError",
                schema: "blacktech",
                table: "Invoices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSentAt",
                schema: "blacktech",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailStatus",
                schema: "blacktech",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailError",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "EmailSentAt",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "EmailStatus",
                schema: "blacktech",
                table: "Invoices");
        }
    }
}
