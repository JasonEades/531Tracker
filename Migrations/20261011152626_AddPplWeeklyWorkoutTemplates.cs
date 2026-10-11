using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPplWeeklyWorkoutTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PplDayTemplates_PplProgramId",
                table: "PplDayTemplates");

            migrationBuilder.AddColumn<int>(
                name: "WeekNumber",
                table: "PplDayTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PplDayTemplates_PplProgramId_WeekNumber_OrderInWeek",
                table: "PplDayTemplates",
                columns: new[] { "PplProgramId", "WeekNumber", "OrderInWeek" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PplDayTemplates_PplProgramId_WeekNumber_OrderInWeek",
                table: "PplDayTemplates");

            migrationBuilder.DropColumn(
                name: "WeekNumber",
                table: "PplDayTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_PplDayTemplates_PplProgramId",
                table: "PplDayTemplates",
                column: "PplProgramId");
        }
    }
}
