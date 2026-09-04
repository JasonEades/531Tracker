using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class UserScopeAccessoryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "AccessoryHistory",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEquipment_UserId",
                table: "UserEquipment",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryHistory_UserId_AccessoryId_RecordedAt",
                table: "AccessoryHistory",
                columns: new[] { "UserId", "AccessoryId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserEquipment_UserId",
                table: "UserEquipment");

            migrationBuilder.DropIndex(
                name: "IX_AccessoryHistory_UserId_AccessoryId_RecordedAt",
                table: "AccessoryHistory");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AccessoryHistory");
        }
    }
}
