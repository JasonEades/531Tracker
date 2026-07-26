using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentAndPlates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEquipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BarWeight = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEquipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlateInventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserEquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    PairsAvailable = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlateInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlateInventory_UserEquipment_UserEquipmentId",
                        column: x => x.UserEquipmentId,
                        principalTable: "UserEquipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlateInventory_UserEquipmentId",
                table: "PlateInventory",
                column: "UserEquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlateInventory");

            migrationBuilder.DropTable(
                name: "UserEquipment");
        }
    }
}
