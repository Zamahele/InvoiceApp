using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecurringPeriod",
                schema: "blacktech",
                table: "Invoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurringScheduleId",
                schema: "blacktech",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurringInvoiceSchedules",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SourceInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientAddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientAddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientPostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VATEnabled = table.Column<bool>(type: "bit", nullable: false),
                    VATRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RetentionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RetentionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RetentionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DueDay = table.Column<int>(type: "int", nullable: false),
                    NextRunDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringInvoiceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringInvoiceSchedules_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "blacktech",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringInvoiceLineItems",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecurringInvoiceScheduleId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringInvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringInvoiceLineItems_RecurringInvoiceSchedules_RecurringInvoiceScheduleId",
                        column: x => x.RecurringInvoiceScheduleId,
                        principalSchema: "blacktech",
                        principalTable: "RecurringInvoiceSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_RecurringScheduleId_RecurringPeriod",
                schema: "blacktech",
                table: "Invoices",
                columns: new[] { "RecurringScheduleId", "RecurringPeriod" },
                unique: true,
                filter: "[RecurringScheduleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringInvoiceLineItems_RecurringInvoiceScheduleId",
                schema: "blacktech",
                table: "RecurringInvoiceLineItems",
                column: "RecurringInvoiceScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringInvoiceSchedules_CompanyId",
                schema: "blacktech",
                table: "RecurringInvoiceSchedules",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringInvoiceSchedules_SourceInvoiceId",
                schema: "blacktech",
                table: "RecurringInvoiceSchedules",
                column: "SourceInvoiceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_RecurringInvoiceSchedules_RecurringScheduleId",
                schema: "blacktech",
                table: "Invoices",
                column: "RecurringScheduleId",
                principalSchema: "blacktech",
                principalTable: "RecurringInvoiceSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_RecurringInvoiceSchedules_RecurringScheduleId",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "RecurringInvoiceLineItems",
                schema: "blacktech");

            migrationBuilder.DropTable(
                name: "RecurringInvoiceSchedules",
                schema: "blacktech");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_RecurringScheduleId_RecurringPeriod",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RecurringPeriod",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RecurringScheduleId",
                schema: "blacktech",
                table: "Invoices");
        }
    }
}
