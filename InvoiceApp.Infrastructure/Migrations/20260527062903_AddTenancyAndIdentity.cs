using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenancyAndIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "blacktech",
                table: "SavedRates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "blacktech",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "blacktech",
                table: "RentPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "blacktech",
                table: "Properties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "blacktech",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "blacktech",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "blacktech",
                table: "CompanySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "blacktech",
                table: "BankingDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvoicingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RentTrackingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "blacktech",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "blacktech",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "blacktech",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "blacktech",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "blacktech",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "blacktech",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "blacktech",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "blacktech",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "blacktech",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "blacktech",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedRates_CompanyId",
                schema: "blacktech",
                table: "SavedRates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_CompanyId",
                schema: "blacktech",
                table: "Rooms",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RentPayments_CompanyId",
                schema: "blacktech",
                table: "RentPayments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_CompanyId",
                schema: "blacktech",
                table: "Properties",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CompanyId",
                schema: "blacktech",
                table: "Invoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySettings_CompanyId",
                schema: "blacktech",
                table: "CompanySettings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankingDetails_CompanyId",
                schema: "blacktech",
                table: "BankingDetails",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "blacktech",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "blacktech",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "blacktech",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "blacktech",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "blacktech",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "blacktech",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyId",
                schema: "blacktech",
                table: "AspNetUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "blacktech",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_BankingDetails_Companies_CompanyId",
                schema: "blacktech",
                table: "BankingDetails",
                column: "CompanyId",
                principalSchema: "blacktech",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanySettings_Companies_CompanyId",
                schema: "blacktech",
                table: "CompanySettings",
                column: "CompanyId",
                principalSchema: "blacktech",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Companies_CompanyId",
                schema: "blacktech",
                table: "Invoices",
                column: "CompanyId",
                principalSchema: "blacktech",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Companies_CompanyId",
                schema: "blacktech",
                table: "Properties",
                column: "CompanyId",
                principalSchema: "blacktech",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RentPayments_Companies_CompanyId",
                schema: "blacktech",
                table: "RentPayments",
                column: "CompanyId",
                principalSchema: "blacktech",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Companies_CompanyId",
                schema: "blacktech",
                table: "Rooms",
                column: "CompanyId",
                principalSchema: "blacktech",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedRates_Companies_CompanyId",
                schema: "blacktech",
                table: "SavedRates",
                column: "CompanyId",
                principalSchema: "blacktech",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankingDetails_Companies_CompanyId",
                schema: "blacktech",
                table: "BankingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanySettings_Companies_CompanyId",
                schema: "blacktech",
                table: "CompanySettings");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Companies_CompanyId",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Companies_CompanyId",
                schema: "blacktech",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_RentPayments_Companies_CompanyId",
                schema: "blacktech",
                table: "RentPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Companies_CompanyId",
                schema: "blacktech",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_SavedRates_Companies_CompanyId",
                schema: "blacktech",
                table: "SavedRates");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "blacktech");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "blacktech");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "blacktech");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "blacktech");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "blacktech");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "blacktech");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "blacktech");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "blacktech");

            migrationBuilder.DropIndex(
                name: "IX_SavedRates_CompanyId",
                schema: "blacktech",
                table: "SavedRates");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_CompanyId",
                schema: "blacktech",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_RentPayments_CompanyId",
                schema: "blacktech",
                table: "RentPayments");

            migrationBuilder.DropIndex(
                name: "IX_Properties_CompanyId",
                schema: "blacktech",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CompanyId",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_CompanySettings_CompanyId",
                schema: "blacktech",
                table: "CompanySettings");

            migrationBuilder.DropIndex(
                name: "IX_BankingDetails_CompanyId",
                schema: "blacktech",
                table: "BankingDetails");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "blacktech",
                table: "SavedRates");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "blacktech",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "blacktech",
                table: "RentPayments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "blacktech",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "blacktech",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "blacktech",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "blacktech",
                table: "BankingDetails");
        }
    }
}
