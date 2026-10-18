using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class ExpandExerciseLibrarySeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ExerciseDefinitions",
                columns: new[] { "Id", "Equipment", "Instructions", "MovementType", "Name", "PrimaryMuscle", "SecondaryMuscles", "UserId" },
                values: new object[,]
                {
                    { -84, "Cable or resistance band", "Stand sideways to a cable or band and hold it at your chest. Press your hands forward while resisting rotation, then return and switch sides.", "Anti-rotation", "Pallof Press", "Core", "Shoulders, glutes", null },
                    { -83, "Pull-up bar", "Hang from a stable bar and brace. Raise your knees toward your torso without swinging, then lower slowly.", "Hip flexion", "Hanging Knee Raise", "Core", "Hip flexors, lats", null },
                    { -82, "Bodyweight", "Support yourself on one forearm and the side of your foot. Lift your hips and keep your body aligned, then switch sides.", "Anti-lateral flexion", "Side Plank", "Core", "Obliques, shoulders, glutes", null },
                    { -81, "Bodyweight", "Support yourself on your forearms and toes. Brace your trunk and keep your body in a straight line without letting your hips sag.", "Anti-extension", "Plank", "Core", "Shoulders, glutes", null },
                    { -80, "Cable, rope attachment", "Kneel facing a high cable and hold the rope by your head. Curl your ribs toward your pelvis, then return without pulling with your arms.", "Trunk flexion", "Cable Crunch", "Core", "Obliques", null },
                    { -77, "Hip adduction machine", "Sit with legs set against the pads. Bring your legs together against resistance, then return to the start under control.", "Hip adduction", "Hip Adduction", "Glutes", "Inner thighs", null },
                    { -76, "Hip abduction machine", "Sit with knees against the pads. Move your knees outward against the resistance, pause briefly, then return slowly.", "Hip abduction", "Hip Abduction", "Glutes", "Hip abductors", null },
                    { -75, "Dumbbell", "Balance on one leg with a soft knee. Hinge at the hip as the free leg reaches back, then return upright without twisting.", "Single-leg hip hinge", "Single-Leg Romanian Deadlift", "Hamstrings", "Glutes, core", null },
                    { -74, "Bench or box, dumbbells optional", "Place one foot fully on a stable step. Drive through that foot to stand on the step, then lower slowly and repeat on the other side.", "Single-leg squat", "Step-Up", "Quads", "Glutes, hamstrings", null },
                    { -73, "Bodyweight or dumbbells", "Step one foot back and lower under control. Push through the front foot to return to standing, then alternate sides.", "Single-leg squat", "Reverse Lunge", "Glutes", "Quads, hamstrings", null },
                    { -72, "Bodyweight or dumbbells", "Step forward into a controlled lunge, lowering until both knees are comfortably bent. Push through the front foot to step into the next lunge.", "Single-leg squat", "Walking Lunge", "Quads", "Glutes, hamstrings", null },
                    { -71, "Bodyweight", "Lie on your back with knees bent and feet planted. Brace and raise your hips until your body forms a straight line from shoulders to knees, then lower slowly.", "Hip extension", "Glute Bridge", "Glutes", "Hamstrings, core", null },
                    { -70, "Barbell, bench", "Rest your upper back against a stable bench with the bar over your hips. Drive through your feet to extend your hips, then lower under control.", "Hip extension", "Barbell Hip Thrust", "Glutes", "Hamstrings, core", null },
                    { -69, "Barbell, rack", "With a secure bar position and soft knees, brace and hinge your hips back while keeping your spine neutral. Stand by extending your hips.", "Hip hinge", "Good Morning", "Hamstrings", "Glutes, back", null },
                    { -68, "Trap bar", "Stand centered in the trap bar and grip the handles. Brace, push through the floor to stand tall, then lower the bar under control.", "Hip hinge", "Trap Bar Deadlift", "Glutes", "Quads, hamstrings, back", null },
                    { -67, "Barbell", "Take a wide stance with toes turned slightly out and grip the bar inside your legs. Brace and push the floor apart as you stand, then lower with control.", "Hip hinge", "Sumo Deadlift", "Glutes", "Quads, hamstrings, back", null },
                    { -66, "Dumbbell or kettlebell", "Hold a weight close to your chest. Squat between your hips with your torso braced, then stand while keeping your feet planted.", "Squat", "Goblet Squat", "Quads", "Glutes, core", null },
                    { -65, "Barbell, rack", "Rest the bar across the front of your shoulders and keep your elbows lifted. Brace, squat to a controlled depth, then stand while keeping your torso upright.", "Squat", "Front Squat", "Quads", "Glutes, core, upper back", null },
                    { -64, "Bodyweight", "Set your hands slightly narrower than shoulder width. Keep your body straight, lower with elbows near your sides, then press up.", "Horizontal push", "Close-Grip Push-Up", "Triceps", "Chest, shoulders, core", null },
                    { -63, "Cable, rope attachment", "Face away from a low cable with the rope behind your head. Extend your elbows overhead, then return slowly while keeping your upper arms steady.", "Elbow extension", "Overhead Cable Tricep Extension", "Triceps", "Shoulders", null },
                    { -62, "EZ bar, bench", "Lie on a bench and hold the bar above your shoulders. Bend your elbows to lower it near your forehead, then extend them to return.", "Elbow extension", "Skull Crusher", "Triceps", "Shoulders", null },
                    { -61, "Dumbbell", "Rest your forearms on your thighs with palms up. Curl your wrists to raise the weights, then lower through a comfortable range.", "Wrist flexion", "Wrist Curl", "Biceps", "Forearms", null },
                    { -60, "Dumbbell, bench", "Sit and brace your upper arm against your inner thigh. Curl the dumbbell toward your shoulder, then lower slowly.", "Elbow flexion", "Concentration Curl", "Biceps", "Forearms", null },
                    { -59, "Preacher bench, barbell", "Rest your upper arms on the pad. Curl the weight without lifting your elbows, then lower until your arms are nearly straight.", "Elbow flexion", "Preacher Curl", "Biceps", "Forearms", null },
                    { -58, "Cable", "Stand facing a high cable with arms nearly straight. Pull the handle toward your thighs while keeping your torso still, then return slowly.", "Shoulder extension", "Straight-Arm Pulldown", "Back", "Triceps, core", null },
                    { -57, "Cable, close-grip handle", "Sit with thighs secured and hold a close-grip handle. Pull toward your upper chest while keeping your torso steady, then return slowly.", "Vertical pull", "Close-Grip Lat Pulldown", "Back", "Biceps, rear delts", null },
                    { -56, "Cable", "Face a cable handle and brace your torso. Pull one elbow back toward your side, then return with control.", "Horizontal pull", "Single-Arm Cable Row", "Back", "Biceps, rear delts", null },
                    { -55, "Cable", "Sit tall and brace. Pull the handle toward your torso by driving your elbows back, then extend your arms without rounding your back.", "Horizontal pull", "Seated Cable Row", "Back", "Biceps, rear delts", null },
                    { -54, "Dumbbell, incline bench", "Lie chest-down on an incline bench. Row the weights toward your torso without lifting your chest, then lower slowly.", "Horizontal pull", "Chest-Supported Row", "Back", "Biceps, rear delts", null },
                    { -53, "Pull-up bar", "Use an overhand grip. Pull until your chin clears the bar, then lower to a controlled hang.", "Vertical pull", "Pull-Up", "Back", "Biceps, forearms", null },
                    { -52, "Dumbbell", "Stand tall holding dumbbells at your sides. Raise your shoulders toward your ears without rolling them, then lower slowly.", "Scapular elevation", "Dumbbell Shrug", "Back", "Traps, forearms", null },
                    { -51, "Barbell", "Use a comfortable grip and raise the bar in front of your torso only as high as your shoulders tolerate. Lower with control.", "Shoulder abduction", "Upright Row", "Shoulders", "Traps, biceps", null },
                    { -50, "Dumbbell, bench", "Start with dumbbells at shoulder height. Brace and press overhead, then lower them under control.", "Vertical push", "Dumbbell Shoulder Press", "Shoulders", "Triceps, upper chest", null },
                    { -49, "Dumbbell", "Raise the weights in front of you to about shoulder height with elbows slightly bent, then lower under control.", "Shoulder flexion", "Front Raise", "Shoulders", "Upper chest", null },
                    { -48, "Cable", "Stand beside a low cable with the handle in your outside hand. Raise your arm out to shoulder height, then lower slowly.", "Shoulder isolation", "Cable Lateral Raise", "Shoulders", "Traps", null },
                    { -47, "Pec deck machine", "Face the machine pads and hold the handles. Move your arms outward with control, then return without letting the weights drop.", "Shoulder horizontal abduction", "Reverse Pec Deck", "RearDelts", "Upper back", null },
                    { -46, "Dumbbell", "Hinge forward with a steady torso. Raise the weights out to the sides with a slight elbow bend, then lower slowly.", "Shoulder horizontal abduction", "Reverse Fly", "RearDelts", "Upper back", null },
                    { -45, "Dumbbell, flat bench", "Lie on a bench with a slight bend in your elbows. Open your arms to a comfortable stretch, then bring the weights together without changing the elbow bend.", "Horizontal adduction", "Dumbbell Fly", "Chest", "Shoulders", null },
                    { -44, "Pec deck machine", "Set the seat so your elbows align with the machine pivot. Bring your arms together with a slight bend, then return under control.", "Horizontal adduction", "Pec Deck", "Chest", "Shoulders", null },
                    { -43, "Chest press machine", "Adjust the seat so the handles are near chest level. Press forward without locking your elbows, then return slowly.", "Horizontal push", "Machine Chest Press", "Chest", "Triceps, shoulders", null },
                    { -42, "Bodyweight", "Keep your body in a straight line. Lower your chest toward the floor with elbows controlled, then press back to the starting position.", "Horizontal push", "Push-Up", "Chest", "Triceps, shoulders, core", null },
                    { -41, "Barbell, bench", "Use a comfortable grip narrower than your regular bench press. Lower the bar under control and press while keeping wrists stacked over elbows.", "Horizontal push", "Close-Grip Bench Press", "Triceps", "Chest, shoulders", null },
                    { -40, "Dumbbell, flat bench", "Lie on a flat bench with dumbbells over your chest. Lower them beside your chest, then press up smoothly.", "Horizontal push", "Flat Dumbbell Press", "Chest", "Triceps, shoulders", null },
                    { -39, "Dumbbell, flat bench", "Lie on a flat bench with dumbbells over your chest. Lower them beside your chest, then press up smoothly.", "Horizontal push", "Dumbbell Bench Press", "Chest", "Triceps, shoulders", null },
                    { -38, "Barbell", "Stand tall with the bar at arm's length. Curl toward your shoulders without swinging, then lower under control.", "Elbow flexion", "Barbell Curl", "Biceps", "Forearms", null },
                    { -37, "Dumbbell, bench", "Start with dumbbells at shoulder height. Brace and press overhead, then lower them under control.", "Vertical push", "DB Shoulder Press", "Shoulders", "Triceps, upper chest", null },
                    { -36, "Pull-up bar", "Hang from a stable bar and brace. Raise your knees or straight legs without swinging, then lower slowly.", "Hip flexion", "Hanging Leg Raise", "Core", "Hip flexors, lats", null },
                    { -35, "Ab wheel", "Kneel with the wheel under your shoulders. Brace your trunk and roll forward only as far as you can control, then return without arching your back.", "Anti-extension", "Ab Wheel", "Core", "Shoulders, lats", null },
                    { -34, "Pull-up bar", "Use an underhand grip. Pull until your chin clears the bar, then lower to a controlled hang.", "Vertical pull", "Chin-Ups", "Back", "Biceps, forearms", null },
                    { -33, "Cable, rope attachment", "Set the cable around face height. Pull the rope toward your face with elbows high, then return while keeping your shoulders controlled.", "Horizontal pull", "Face Pull", "RearDelts", "Upper back, external rotators", null },
                    { -32, "Dumbbell, bench", "Support one hand and knee on a bench. Pull the dumbbell toward your hip while keeping your torso steady, then lower it under control.", "Horizontal pull", "Dumbbell Row", "Back", "Biceps, rear delts", null }
                });

            migrationBuilder.Sql("""
                UPDATE "PplExerciseSlots"
                SET "ExerciseDefinitionId" = (
                    SELECT "Id"
                    FROM "ExerciseDefinitions"
                    WHERE "UserId" IS NULL
                      AND "Name" = "PplExerciseSlots"."ExerciseName")
                WHERE "ExerciseDefinitionId" IS NULL
                  AND EXISTS (
                    SELECT 1
                    FROM "ExerciseDefinitions"
                    WHERE "UserId" IS NULL
                      AND "Name" = "PplExerciseSlots"."ExerciseName");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -84);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -83);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -82);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -81);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -80);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -77);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -76);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -75);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -74);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -73);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -72);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -71);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -70);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -69);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -68);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -67);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -66);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -65);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -64);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -63);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -62);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -61);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -60);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -59);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -58);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -57);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -56);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -55);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -54);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -53);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -52);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -51);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -50);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -49);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -48);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -47);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -46);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -45);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -44);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -43);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -42);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -41);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -40);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -39);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -38);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -37);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -36);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -35);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -34);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -33);

            migrationBuilder.DeleteData(
                table: "ExerciseDefinitions",
                keyColumn: "Id",
                keyValue: -32);
        }
    }
}
