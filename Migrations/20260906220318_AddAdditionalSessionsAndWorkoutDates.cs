using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalSessionsAndWorkoutDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workouts_WeekId",
                table: "Workouts");

            migrationBuilder.DropIndex(
                name: "IX_PplSessions_PplProgramId",
                table: "PplSessions");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Workouts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredOn",
                table: "Workouts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredOn",
                table: "PplSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "PplWeekId",
                table: "PplSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Accessories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"Workouts\" SET \"OccurredOn\" = COALESCE(\"CompletedAt\", (SELECT \"CreatedAt\" FROM \"Cycles\" c JOIN \"Weeks\" w ON w.\"CycleId\" = c.\"Id\" WHERE w.\"Id\" = \"Workouts\".\"WeekId\")), \"CreatedAt\" = COALESCE(\"CompletedAt\", (SELECT \"CreatedAt\" FROM \"Cycles\" c JOIN \"Weeks\" w ON w.\"CycleId\" = c.\"Id\" WHERE w.\"Id\" = \"Workouts\".\"WeekId\"));");
            migrationBuilder.Sql("UPDATE \"PplSessions\" SET \"OccurredOn\" = COALESCE(\"StartedAt\", \"CompletedAt\", \"CreatedAt\");");
            migrationBuilder.Sql("UPDATE \"Accessories\" SET \"Category\" = 'Other' WHERE \"Category\" = '';");

            migrationBuilder.CreateTable(
                name: "PplWeek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PplProgramId = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PplWeek", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PplWeek_PplPrograms_PplProgramId",
                        column: x => x.PplProgramId,
                        principalTable: "PplPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeekId = table.Column<int>(type: "integer", nullable: true),
                    PplWeekId = table.Column<int>(type: "integer", nullable: true),
                    SessionType = table.Column<string>(type: "text", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalSession_PplWeek_PplWeekId",
                        column: x => x.PplWeekId,
                        principalTable: "PplWeek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdditionalSession_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalStrengthExercise",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdditionalSessionId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalStrengthExercise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalStrengthExercise_AdditionalSession_AdditionalSess~",
                        column: x => x.AdditionalSessionId,
                        principalTable: "AdditionalSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CardioEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdditionalSessionId = table.Column<int>(type: "integer", nullable: false),
                    AccessoryId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardioEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardioEntry_Accessories_AccessoryId",
                        column: x => x.AccessoryId,
                        principalTable: "Accessories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardioEntry_AdditionalSession_AdditionalSessionId",
                        column: x => x.AdditionalSessionId,
                        principalTable: "AdditionalSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalStrengthSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdditionalStrengthExerciseId = table.Column<int>(type: "integer", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: true),
                    Reps = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalStrengthSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalStrengthSet_AdditionalStrengthExercise_Additional~",
                        column: x => x.AdditionalStrengthExerciseId,
                        principalTable: "AdditionalStrengthExercise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 1,
                column: "Category",
                value: "Strength");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 2,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 3,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 4,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 5,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 6,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 7,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 8,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 9,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 10,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 11,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 12,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 13,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 14,
                column: "Category",
                value: "Other");

            migrationBuilder.UpdateData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 15,
                column: "Category",
                value: "Strength");

            migrationBuilder.InsertData(
                table: "Accessories",
                columns: new[] { "Id", "Category", "Description", "IsActive", "Name", "UserId" },
                values: new object[,]
                {
                    { 16, "Cardio", null, true, "Rowing", null },
                    { 17, "Cardio", null, true, "Biking", null },
                    { 18, "Cardio", null, true, "Walking", null },
                    { 19, "Cardio", null, true, "Running", null },
                    { 20, "Cardio", null, true, "Treadmill", null },
                    { 21, "Cardio", null, true, "Elliptical", null },
                    { 22, "Cardio", null, true, "Stair Climber", null },
                    { 23, "Cardio", null, true, "Swimming", null },
                    { 24, "Cardio", null, true, "Rucking", null },
                    { 25, "Cardio", null, true, "Other", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_WeekId_OccurredOn",
                table: "Workouts",
                columns: new[] { "WeekId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PplSessions_PplProgramId_OccurredOn",
                table: "PplSessions",
                columns: new[] { "PplProgramId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PplSessions_PplWeekId",
                table: "PplSessions",
                column: "PplWeekId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalSession_PplWeekId_OccurredOn",
                table: "AdditionalSession",
                columns: new[] { "PplWeekId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalSession_WeekId_OccurredOn",
                table: "AdditionalSession",
                columns: new[] { "WeekId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalStrengthExercise_AdditionalSessionId",
                table: "AdditionalStrengthExercise",
                column: "AdditionalSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalStrengthSet_AdditionalStrengthExerciseId",
                table: "AdditionalStrengthSet",
                column: "AdditionalStrengthExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_CardioEntry_AccessoryId",
                table: "CardioEntry",
                column: "AccessoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CardioEntry_AdditionalSessionId",
                table: "CardioEntry",
                column: "AdditionalSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PplWeek_PplProgramId_WeekNumber",
                table: "PplWeek",
                columns: new[] { "PplProgramId", "WeekNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PplSessions_PplWeek_PplWeekId",
                table: "PplSessions",
                column: "PplWeekId",
                principalTable: "PplWeek",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PplSessions_PplWeek_PplWeekId",
                table: "PplSessions");

            migrationBuilder.DropTable(
                name: "AdditionalStrengthSet");

            migrationBuilder.DropTable(
                name: "CardioEntry");

            migrationBuilder.DropTable(
                name: "AdditionalStrengthExercise");

            migrationBuilder.DropTable(
                name: "AdditionalSession");

            migrationBuilder.DropTable(
                name: "PplWeek");

            migrationBuilder.DropIndex(
                name: "IX_Workouts_WeekId_OccurredOn",
                table: "Workouts");

            migrationBuilder.DropIndex(
                name: "IX_PplSessions_PplProgramId_OccurredOn",
                table: "PplSessions");

            migrationBuilder.DropIndex(
                name: "IX_PplSessions_PplWeekId",
                table: "PplSessions");

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "OccurredOn",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "OccurredOn",
                table: "PplSessions");

            migrationBuilder.DropColumn(
                name: "PplWeekId",
                table: "PplSessions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Accessories");

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_WeekId",
                table: "Workouts",
                column: "WeekId");

            migrationBuilder.CreateIndex(
                name: "IX_PplSessions_PplProgramId",
                table: "PplSessions",
                column: "PplProgramId");
        }
    }
}
