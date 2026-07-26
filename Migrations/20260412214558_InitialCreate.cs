using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CycleNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LiftType = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TrainingMax = table.Column<double>(type: "REAL", nullable: false),
                    BbbPercentage = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessoryHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccessoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: false),
                    Sets = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessoryHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessoryHistory_Accessories_AccessoryId",
                        column: x => x.AccessoryId,
                        principalTable: "Accessories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Weeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CycleId = table.Column<int>(type: "INTEGER", nullable: false),
                    WeekNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Weeks_Cycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "Cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WeekId = table.Column<int>(type: "INTEGER", nullable: false),
                    MainLiftType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workouts_Weeks_WeekId",
                        column: x => x.WeekId,
                        principalTable: "Weeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutAccessories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    Reps = table.Column<int>(type: "INTEGER", nullable: false),
                    Sets = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutAccessories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutAccessories_Accessories_AccessoryId",
                        column: x => x.AccessoryId,
                        principalTable: "Accessories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutAccessories_Workouts_WorkoutId",
                        column: x => x.WorkoutId,
                        principalTable: "Workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkoutId = table.Column<int>(type: "INTEGER", nullable: false),
                    LiftId = table.Column<int>(type: "INTEGER", nullable: false),
                    SetType = table.Column<string>(type: "TEXT", nullable: false),
                    SetNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    PrescribedWeight = table.Column<double>(type: "REAL", nullable: false),
                    PrescribedReps = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualWeight = table.Column<double>(type: "REAL", nullable: true),
                    ActualReps = table.Column<int>(type: "INTEGER", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutSets_Lifts_LiftId",
                        column: x => x.LiftId,
                        principalTable: "Lifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutSets_Workouts_WorkoutId",
                        column: x => x.WorkoutId,
                        principalTable: "Workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Accessories",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "Bent-over barbell row", true, "Barbell Row" },
                    { 2, "Single-arm dumbbell row", true, "Dumbbell Row" },
                    { 3, "Cable lat pulldown", true, "Lat Pulldown" },
                    { 4, "Cable face pull", true, "Face Pull" },
                    { 5, "Parallel bar dips", true, "Dips" },
                    { 6, "Chin-ups / pull-ups", true, "Chin-Ups" },
                    { 7, "Lying or seated leg curl", true, "Leg Curl" },
                    { 8, "Machine leg press", true, "Leg Press" },
                    { 9, "Ab wheel rollout", true, "Ab Wheel" },
                    { 10, "Hanging leg raise", true, "Hanging Leg Raise" },
                    { 11, "Standing dumbbell curl", true, "Dumbbell Curl" },
                    { 12, "Cable tricep pushdown", true, "Tricep Pushdown" },
                    { 13, "Dumbbell lateral raise", true, "Lateral Raise" },
                    { 14, "Romanian deadlift", true, "Romanian Deadlift" },
                    { 15, "Rear foot elevated split squat", true, "Bulgarian Split Squat" }
                });

            migrationBuilder.InsertData(
                table: "Lifts",
                columns: new[] { "Id", "BbbPercentage", "LiftType", "Name", "TrainingMax" },
                values: new object[,]
                {
                    { 1, 50.0, "Squat", "Squat", 315.0 },
                    { 2, 50.0, "BenchPress", "Bench Press", 225.0 },
                    { 3, 50.0, "Deadlift", "Deadlift", 365.0 },
                    { 4, 50.0, "OverheadPress", "Overhead Press", 145.0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessoryHistory_AccessoryId",
                table: "AccessoryHistory",
                column: "AccessoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Lifts_LiftType",
                table: "Lifts",
                column: "LiftType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Weeks_CycleId",
                table: "Weeks",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutAccessories_AccessoryId",
                table: "WorkoutAccessories",
                column: "AccessoryId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutAccessories_WorkoutId",
                table: "WorkoutAccessories",
                column: "WorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_WeekId",
                table: "Workouts",
                column: "WeekId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_LiftId",
                table: "WorkoutSets",
                column: "LiftId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSets_WorkoutId",
                table: "WorkoutSets",
                column: "WorkoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessoryHistory");

            migrationBuilder.DropTable(
                name: "WorkoutAccessories");

            migrationBuilder.DropTable(
                name: "WorkoutSets");

            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropTable(
                name: "Lifts");

            migrationBuilder.DropTable(
                name: "Workouts");

            migrationBuilder.DropTable(
                name: "Weeks");

            migrationBuilder.DropTable(
                name: "Cycles");
        }
    }
}
