using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSettingsAndClientEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientEmail",
                schema: "blacktech",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailSettings",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SmtpHost = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    UseSsl = table.Column<bool>(type: "bit", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "blacktech",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSettings_CompanyId",
                schema: "blacktech",
                table: "EmailSettings",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSettings",
                schema: "blacktech");

            migrationBuilder.DropColumn(
                name: "ClientEmail",
                schema: "blacktech",
                table: "Invoices");
        }
    }
}
