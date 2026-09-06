using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleLevelAdditionalSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CycleId",
                table: "AdditionalSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalSessions_CycleId",
                table: "AdditionalSessions",
                column: "CycleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalSessions_Cycles_CycleId",
                table: "AdditionalSessions",
                column: "CycleId",
                principalTable: "Cycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalSessions_Cycles_CycleId",
                table: "AdditionalSessions");

            migrationBuilder.DropIndex(
                name: "IX_AdditionalSessions_CycleId",
                table: "AdditionalSessions");

            migrationBuilder.DropColumn(
                name: "CycleId",
                table: "AdditionalSessions");
        }
    }
}
