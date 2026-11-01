using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionOnlyPplExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PplExerciseSlotId",
                table: "PplSessionExercises",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "AddedIsBodyweight",
                table: "PplSessionExercises",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AddedMuscleGroup",
                table: "PplSessionExercises",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddedIsBodyweight",
                table: "PplSessionExercises");

            migrationBuilder.DropColumn(
                name: "AddedMuscleGroup",
                table: "PplSessionExercises");

            migrationBuilder.AlterColumn<int>(
                name: "PplExerciseSlotId",
                table: "PplSessionExercises",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
