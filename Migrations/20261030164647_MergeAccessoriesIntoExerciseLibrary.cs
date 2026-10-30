using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class MergeAccessoriesIntoExerciseLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repoint PPL slots at the surviving library entry before the duplicate rows are removed.
            // Slot/session exercise names are untouched, so training history keeps its original names.
            migrationBuilder.Sql("""UPDATE "PplExerciseSlots" SET "ExerciseDefinitionId" = -13 WHERE "ExerciseDefinitionId" = -33;""");
            migrationBuilder.Sql("""UPDATE "PplExerciseSlots" SET "ExerciseDefinitionId" = -39 WHERE "ExerciseDefinitionId" = -40;""");
            migrationBuilder.Sql("""UPDATE "PplExerciseSlots" SET "ExerciseDefinitionId" = -37 WHERE "ExerciseDefinitionId" = -50;""");
            migrationBuilder.Sql("""UPDATE "PplExerciseSlots" SET "ExerciseDefinitionId" = -10 WHERE "ExerciseDefinitionId" = -55;""");
            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -55);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -50);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -40);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -33);

            migrationBuilder.AddColumn<int>(
                name: "ExerciseDefinitionId",
                table: "Accessories",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 1,
                column: "ExerciseDefinitionId",
                value: -9);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 2,
                column: "ExerciseDefinitionId",
                value: -32);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 3,
                column: "ExerciseDefinitionId",
                value: -8);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ExerciseDefinitionId", "Name" },
                values: new object[] { -13, "Face Pulls" });

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 5,
                column: "ExerciseDefinitionId",
                value: -27);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 6,
                column: "ExerciseDefinitionId",
                value: -34);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 7,
                column: "ExerciseDefinitionId",
                value: -19);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 8,
                column: "ExerciseDefinitionId",
                value: -18);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 9,
                column: "ExerciseDefinitionId",
                value: -35);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 10,
                column: "ExerciseDefinitionId",
                value: -36);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 11,
                column: "ExerciseDefinitionId",
                value: -11);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 12,
                column: "ExerciseDefinitionId",
                value: -5);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 13,
                column: "ExerciseDefinitionId",
                value: -4);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 14,
                column: "ExerciseDefinitionId",
                value: -17);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 15,
                column: "ExerciseDefinitionId",
                value: -22);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 16,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 17,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 18,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 19,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 20,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 21,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 22,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 23,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 24,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 25,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 26,
                column: "ExerciseDefinitionId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Accessories_ExerciseDefinitionId",
                table: "Accessories",
                column: "ExerciseDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accessories_ExerciseDefinitions_ExerciseDefinitionId",
                table: "Accessories",
                column: "ExerciseDefinitionId",
                principalTable: "ExerciseDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Link user-created accessories to the matching library entry (own custom exercise first, then the catalog).
            migrationBuilder.Sql("""
                UPDATE "Accessories" SET "ExerciseDefinitionId" = CASE lower("Name")
                    WHEN 'face pull' THEN -13
                    WHEN 'flat dumbbell press' THEN -39
                    WHEN 'dumbbell shoulder press' THEN -37
                    WHEN 'seated cable row' THEN -10
                END
                WHERE "ExerciseDefinitionId" IS NULL AND "Category" <> 'Cardio'
                  AND lower("Name") IN ('face pull', 'flat dumbbell press', 'dumbbell shoulder press', 'seated cable row');
                """);
            migrationBuilder.Sql("""
                UPDATE "Accessories" SET "ExerciseDefinitionId" = (
                    SELECT d."Id" FROM "ExerciseDefinitions" d
                    WHERE lower(d."Name") = lower("Accessories"."Name")
                      AND (d."UserId" IS NULL OR d."UserId" = "Accessories"."UserId")
                    ORDER BY CASE WHEN d."UserId" IS NULL THEN 1 ELSE 0 END, d."Id"
                    LIMIT 1)
                WHERE "ExerciseDefinitionId" IS NULL AND "Category" <> 'Cardio';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accessories_ExerciseDefinitions_ExerciseDefinitionId",
                table: "Accessories");

            migrationBuilder.DropIndex(
                name: "IX_Accessories_ExerciseDefinitionId",
                table: "Accessories");

            migrationBuilder.DropColumn(
                name: "ExerciseDefinitionId",
                table: "Accessories");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Face Pull");

            migrationBuilder.InsertData(
                table: "ExerciseDefinitions",
                columns: new[] { "Id", "Equipment", "Instructions", "MovementType", "Name", "PrimaryMuscle", "SecondaryMuscles", "UserId" },
                values: new object[,]
                {
                    { -55, "Cable", "Sit tall and brace. Pull the handle toward your torso by driving your elbows back, then extend your arms without rounding your back.", "Horizontal pull", "Seated Cable Row", "Back", "Biceps, rear delts", null },
                    { -50, "Dumbbell, bench", "Start with dumbbells at shoulder height. Brace and press overhead, then lower them under control.", "Vertical push", "Dumbbell Shoulder Press", "Shoulders", "Triceps, upper chest", null },
                    { -40, "Dumbbell, flat bench", "Lie on a flat bench with dumbbells over your chest. Lower them beside your chest, then press up smoothly.", "Horizontal push", "Flat Dumbbell Press", "Chest", "Triceps, shoulders", null },
                    { -33, "Cable, rope attachment", "Set the cable around face height. Pull the rope toward your face with elbows high, then return while keeping your shoulders controlled.", "Horizontal pull", "Face Pull", "RearDelts", "Upper back, external rotators", null }
                });
        }
    }
}
