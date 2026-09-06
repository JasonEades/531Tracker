using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutAndTrainingNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Workouts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Weeks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Cycles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Weeks");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Cycles");
        }
    }
}
