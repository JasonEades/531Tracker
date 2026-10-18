using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExerciseDefinitionId",
                table: "PplExerciseSlots",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExerciseDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryMuscle = table.Column<string>(type: "text", nullable: false),
                    SecondaryMuscles = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Equipment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MovementType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseDefinitions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ExerciseDefinitions",
                columns: new[] { "Id", "Equipment", "Instructions", "MovementType", "Name", "PrimaryMuscle", "SecondaryMuscles", "UserId" },
                values: new object[,]
                {
                    { -1, "Barbell", "Lie on the bench with feet planted. Lower the bar under control to the mid-chest, then press it upward until your arms are extended.", "Horizontal push", "Barbell Bench Press", "Chest", "Triceps, shoulders", null },
                    { -2, "Dumbbell, incline bench", "Set the bench to a low incline. Lower the dumbbells beside the upper chest, then press them up without letting them collide.", "Incline push", "Incline DB Press", "Chest", "Triceps, shoulders", null },
                    { -3, "Barbell", "Start with the bar at shoulder height. Brace your trunk and press overhead, moving your head clear of the bar.", "Vertical push", "Overhead Press", "Shoulders", "Triceps, upper chest", null },
                    { -4, "Dumbbell", "With a slight bend in your elbows, raise the weights out to your sides to about shoulder height, then lower them under control.", "Shoulder isolation", "Lateral Raise", "Shoulders", "Traps", null },
                    { -5, "Cable", "Keep your elbows near your sides. Extend your arms against the cable, then return slowly without moving your upper arms.", "Elbow extension", "Tricep Pushdown", "Triceps", "Forearms", null },
                    { -6, "Dumbbell", "Hold one weight overhead with elbows bent. Extend your elbows to raise the weight, then lower it behind your head with control.", "Elbow extension", "Overhead Tricep Ext", "Triceps", "Shoulders", null },
                    { -7, "Barbell", "Stand with the bar over midfoot. Brace, hinge and bend to grip the bar, then stand by pushing through the floor while keeping the bar close.", "Hip hinge", "Conventional Deadlift", "Back", "Glutes, hamstrings, quads, core", null },
                    { -8, "Cable", "Grip the bar wider than shoulder width. Pull it toward your upper chest while keeping your torso steady, then return with control.", "Vertical pull", "Lat Pulldown", "Back", "Biceps, rear delts", null },
                    { -9, "Barbell", "Hinge forward with a braced, neutral torso. Pull the bar toward your lower ribs, then lower it without changing your torso angle.", "Horizontal pull", "Barbell Row", "Back", "Biceps, rear delts, core", null },
                    { -10, "Cable", "Sit tall and brace. Pull the handle toward your torso by driving your elbows back, then extend your arms without rounding your back.", "Horizontal pull", "Cable Row", "Back", "Biceps, rear delts", null },
                    { -11, "Dumbbell", "Keep your upper arms still and curl the weights toward your shoulders. Lower them slowly to the starting position.", "Elbow flexion", "Dumbbell Curl", "Biceps", "Forearms", null },
                    { -12, "Dumbbell", "Hold dumbbells with palms facing each other. Curl without swinging, then lower under control.", "Elbow flexion", "Hammer Curl", "Biceps", "Forearms", null },
                    { -13, "Cable", "Set the cable around face height. Pull the rope toward your face with elbows high, then return while keeping your shoulders controlled.", "Horizontal pull", "Face Pulls", "RearDelts", "Upper back, external rotators", null },
                    { -14, "Dip bars", "Support yourself on the bars and lower by bending your elbows to a comfortable depth. Press back up without shrugging.", "Vertical push", "Tricep Dips", "Triceps", "Chest, shoulders", null },
                    { -15, "Pull-up bar, weight", "Use a secure added load if needed. Pull until your chin clears the bar, then lower to a controlled hang.", "Vertical pull", "Weighted Pull-ups", "Back", "Biceps, forearms", null },
                    { -16, "Barbell, rack", "Set the bar securely across your upper back. Brace, sit between your hips to a controlled depth, then stand while keeping your feet planted.", "Squat", "Back Squat", "Quads", "Glutes, hamstrings, core", null },
                    { -17, "Barbell", "Hold the bar against your thighs. Push your hips back with a slight knee bend, lower to a strong hamstring stretch, then stand by extending your hips.", "Hip hinge", "Romanian Deadlift", "Hamstrings", "Glutes, back", null },
                    { -18, "Leg press machine", "Set your feet comfortably on the platform. Lower the sled under control to a pain-free depth, then press through your feet without locking your knees.", "Squat", "Leg Press", "Quads", "Glutes, hamstrings", null },
                    { -19, "Leg curl machine", "Align your knees with the machine pivot. Curl the pad toward you, pause briefly, then return slowly.", "Knee flexion", "Leg Curl", "Hamstrings", "Calves", null },
                    { -20, "Calf raise machine", "Lower your heels to a comfortable stretch, then rise onto the balls of your feet. Use a controlled range without bouncing.", "Plantar flexion", "Standing Calf Raise", "Calves", "Soleus", null },
                    { -21, "Hack squat machine", "Set your shoulders against the pads and feet securely on the platform. Lower to a controlled depth, then press back up smoothly.", "Squat", "Hack Squat", "Quads", "Glutes", null },
                    { -22, "Dumbbell, bench", "Place your rear foot on a stable bench. Lower under control by bending the front leg, then drive through the front foot to stand.", "Single-leg squat", "Bulgarian Split Squat", "Glutes", "Quads, hamstrings", null },
                    { -23, "Leg extension machine", "Align your knees with the machine pivot. Extend your knees smoothly, then lower the pad under control.", "Knee extension", "Leg Extension", "Quads", "", null },
                    { -24, "Leg curl machine", "Adjust the pads so your knees align with the pivot. Curl the lower pad down and back, then return slowly.", "Knee flexion", "Seated Leg Curl", "Hamstrings", "Calves", null },
                    { -25, "Barbell, incline bench", "Set the bench to a low incline. Lower the bar under control to the upper chest, then press to full extension.", "Incline push", "Incline Barbell Bench", "Chest", "Triceps, shoulders", null },
                    { -26, "Cable", "Stand between the pulleys with a slight elbow bend. Bring your hands together in front of your chest, then return until you feel a comfortable stretch.", "Horizontal adduction", "Cable Fly", "Chest", "Shoulders", null },
                    { -27, "Dip bars", "Support yourself on the bars and lower by bending your elbows to a comfortable depth. Press back up without shrugging.", "Vertical push", "Dips", "Triceps", "Chest, shoulders", null },
                    { -28, "T-bar row machine", "Brace your torso and pull the handle toward your abdomen. Lower the load with control while keeping your back stable.", "Horizontal pull", "T-Bar Row", "Back", "Biceps, rear delts", null },
                    { -29, "Dumbbell, incline bench", "Sit against an incline bench with arms hanging. Curl the dumbbells without swinging, then lower fully under control.", "Elbow flexion", "Incline DB Curl", "Biceps", "Forearms", null },
                    { -31, "Seated calf raise machine", "Lower your heels under control, then raise them as high as comfortable. Pause briefly and avoid bouncing.", "Plantar flexion", "Seated Calf Raise", "Calves", "Soleus", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PplExerciseSlots_ExerciseDefinitionId",
                table: "PplExerciseSlots",
                column: "ExerciseDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDefinitions_UserId",
                table: "ExerciseDefinitions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PplExerciseSlots_ExerciseDefinitions_ExerciseDefinitionId",
                table: "PplExerciseSlots",
                column: "ExerciseDefinitionId",
                principalTable: "ExerciseDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                UPDATE "PplExerciseSlots"
                SET "ExerciseDefinitionId" = (
                    SELECT "Id"
                    FROM "ExerciseDefinitions"
                    WHERE "UserId" IS NULL
                      AND "Name" = "PplExerciseSlots"."ExerciseName")
                WHERE EXISTS (
                    SELECT 1
                    FROM "ExerciseDefinitions"
                    WHERE "UserId" IS NULL
                      AND "Name" = "PplExerciseSlots"."ExerciseName");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PplExerciseSlots_ExerciseDefinitions_ExerciseDefinitionId",
                table: "PplExerciseSlots");

            migrationBuilder.DropTable(
                name: "ExerciseDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_PplExerciseSlots_ExerciseDefinitionId",
                table: "PplExerciseSlots");

            migrationBuilder.DropColumn(
                name: "ExerciseDefinitionId",
                table: "PplExerciseSlots");
        }
    }
}
