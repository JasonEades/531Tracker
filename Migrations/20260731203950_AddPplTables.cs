using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPplTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PplPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DaysPerWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PplPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PplDayTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PplProgramId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayType = table.Column<string>(type: "TEXT", nullable: false),
                    Variant = table.Column<string>(type: "TEXT", nullable: false),
                    OrderInWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PplDayTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PplDayTemplates_PplPrograms_PplProgramId",
                        column: x => x.PplProgramId,
                        principalTable: "PplPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PplExerciseSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PplDayTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderInDay = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MuscleGroup = table.Column<string>(type: "TEXT", nullable: false),
                    TargetSets = table.Column<int>(type: "INTEGER", nullable: false),
                    RepsMin = table.Column<int>(type: "INTEGER", nullable: false),
                    RepsMax = table.Column<int>(type: "INTEGER", nullable: false),
                    UsePercentageOfTm = table.Column<bool>(type: "INTEGER", nullable: false),
                    TmPercentage = table.Column<double>(type: "REAL", nullable: false),
                    LiftId = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentWeight = table.Column<double>(type: "REAL", nullable: false),
                    ProgressionIncrement = table.Column<double>(type: "REAL", nullable: false),
                    IsBodyweight = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PplExerciseSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PplExerciseSlots_Lifts_LiftId",
                        column: x => x.LiftId,
                        principalTable: "Lifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PplExerciseSlots_PplDayTemplates_PplDayTemplateId",
                        column: x => x.PplDayTemplateId,
                        principalTable: "PplDayTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PplSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PplProgramId = table.Column<int>(type: "INTEGER", nullable: false),
                    PplDayTemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PplSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PplSessions_PplDayTemplates_PplDayTemplateId",
                        column: x => x.PplDayTemplateId,
                        principalTable: "PplDayTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PplSessions_PplPrograms_PplProgramId",
                        column: x => x.PplProgramId,
                        principalTable: "PplPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PplSessionExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PplSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    PplExerciseSlotId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TargetSets = table.Column<int>(type: "INTEGER", nullable: false),
                    RepsMin = table.Column<int>(type: "INTEGER", nullable: false),
                    RepsMax = table.Column<int>(type: "INTEGER", nullable: false),
                    SuggestedWeight = table.Column<double>(type: "REAL", nullable: false),
                    OrderInSession = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PplSessionExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PplSessionExercises_PplExerciseSlots_PplExerciseSlotId",
                        column: x => x.PplExerciseSlotId,
                        principalTable: "PplExerciseSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PplSessionExercises_PplSessions_PplSessionId",
                        column: x => x.PplSessionId,
                        principalTable: "PplSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PplSessionSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PplSessionExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    SetNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetReps = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualReps = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualWeight = table.Column<double>(type: "REAL", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PplSessionSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PplSessionSets_PplSessionExercises_PplSessionExerciseId",
                        column: x => x.PplSessionExerciseId,
                        principalTable: "PplSessionExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PplDayTemplates_PplProgramId",
                table: "PplDayTemplates",
                column: "PplProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_PplExerciseSlots_LiftId",
                table: "PplExerciseSlots",
                column: "LiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PplExerciseSlots_PplDayTemplateId",
                table: "PplExerciseSlots",
                column: "PplDayTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PplSessionExercises_PplExerciseSlotId",
                table: "PplSessionExercises",
                column: "PplExerciseSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_PplSessionExercises_PplSessionId",
                table: "PplSessionExercises",
                column: "PplSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PplSessions_PplDayTemplateId",
                table: "PplSessions",
                column: "PplDayTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PplSessions_PplProgramId",
                table: "PplSessions",
                column: "PplProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_PplSessionSets_PplSessionExerciseId",
                table: "PplSessionSets",
                column: "PplSessionExerciseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PplSessionSets");

            migrationBuilder.DropTable(
                name: "PplSessionExercises");

            migrationBuilder.DropTable(
                name: "PplExerciseSlots");

            migrationBuilder.DropTable(
                name: "PplSessions");

            migrationBuilder.DropTable(
                name: "PplDayTemplates");

            migrationBuilder.DropTable(
                name: "PplPrograms");
        }
    }
}
