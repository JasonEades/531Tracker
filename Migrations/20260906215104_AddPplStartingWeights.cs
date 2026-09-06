using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPplStartingWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "SuggestedWeight",
                table: "PplSessionExercises",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<double>(
                name: "CurrentWeight",
                table: "PplExerciseSlots",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<double>(
                name: "StartingWeight",
                table: "PplExerciseSlots",
                type: "double precision",
                nullable: true);

            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("UPDATE PplExerciseSlots SET CurrentWeight = (SELECT ROUND((L.TrainingMax * PplExerciseSlots.TmPercentage) / 5.0) * 5.0 FROM Lifts L WHERE L.Id = PplExerciseSlots.LiftId) WHERE CurrentWeight = 0 AND UsePercentageOfTm = 1 AND LiftId IS NOT NULL;");
            }
            else
            {
                migrationBuilder.Sql("UPDATE \"PplExerciseSlots\" AS S SET \"CurrentWeight\" = ROUND((L.\"TrainingMax\" * S.\"TmPercentage\") / 5.0) * 5.0 FROM \"Lifts\" AS L WHERE L.\"Id\" = S.\"LiftId\" AND S.\"CurrentWeight\" = 0 AND S.\"UsePercentageOfTm\" = TRUE AND S.\"LiftId\" IS NOT NULL;");
            }

            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("UPDATE PplExerciseSlots SET StartingWeight = CurrentWeight WHERE CurrentWeight IS NOT NULL AND CurrentWeight > 0;");
            }
            else
            {
                migrationBuilder.Sql("UPDATE \"PplExerciseSlots\" SET \"StartingWeight\" = \"CurrentWeight\" WHERE \"CurrentWeight\" IS NOT NULL AND \"CurrentWeight\" > 0;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartingWeight",
                table: "PplExerciseSlots");

            migrationBuilder.AlterColumn<double>(
                name: "SuggestedWeight",
                table: "PplSessionExercises",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CurrentWeight",
                table: "PplExerciseSlots",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);
        }
    }
}
