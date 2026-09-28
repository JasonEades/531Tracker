using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRestSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestSeconds",
                table: "Workouts",
                type: "integer",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<int>(
                name: "RestSeconds",
                table: "PplSessions",
                type: "integer",
                nullable: false,
                defaultValue: 90);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestSeconds",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "RestSeconds",
                table: "PplSessions");
        }
    }
}
