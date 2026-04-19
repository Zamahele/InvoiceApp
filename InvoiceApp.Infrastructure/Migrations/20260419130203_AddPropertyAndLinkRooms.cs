using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyAndLinkRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentSettings",
                schema: "blacktech");

            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                schema: "blacktech",
                table: "Rooms",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Properties",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgentPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_PropertyId",
                schema: "blacktech",
                table: "Rooms",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Properties_PropertyId",
                schema: "blacktech",
                table: "Rooms",
                column: "PropertyId",
                principalSchema: "blacktech",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Properties_PropertyId",
                schema: "blacktech",
                table: "Rooms");

            migrationBuilder.DropTable(
                name: "Properties",
                schema: "blacktech");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_PropertyId",
                schema: "blacktech",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                schema: "blacktech",
                table: "Rooms");

            migrationBuilder.CreateTable(
                name: "RentSettings",
                schema: "blacktech",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgentPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PropertyName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentSettings", x => x.Id);
                });
        }
    }
}
