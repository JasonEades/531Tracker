using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalWorkoutSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutSets_WorkoutId",
                table: "WorkoutSets");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalSetType",
                table: "WorkoutSets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdditional",
                table: "WorkoutSets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutSets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rir",
                table: "WorkoutSets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rpe",
                table: "WorkoutSets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "WorkoutSets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutId_LiftId_IsAdditional_Sequence",
                table: "WorkoutSets",
                columns: new[] { "WorkoutId", "LiftId", "IsAdditional", "Sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutSets_WorkoutId_LiftId_IsAdditional_Sequence",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "AdditionalSetType",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "IsAdditional",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "Rir",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "Rpe",
                table: "WorkoutSets");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "WorkoutSets");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutId",
                table: "WorkoutSets",
                column: "WorkoutId");
        }
    }
}
