using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutAccessoryNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalSession_PplWeek_PplWeekId",
                table: "AdditionalSession");

            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalSession_Weeks_WeekId",
                table: "AdditionalSession");

            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalStrengthExercise_AdditionalSession_AdditionalSess~",
                table: "AdditionalStrengthExercise");

            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalStrengthSet_AdditionalStrengthExercise_Additional~",
                table: "AdditionalStrengthSet");

            migrationBuilder.DropForeignKey(
                name: "FK_CardioEntry_Accessories_AccessoryId",
                table: "CardioEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_CardioEntry_AdditionalSession_AdditionalSessionId",
                table: "CardioEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_PplSessions_PplWeek_PplWeekId",
                table: "PplSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PplWeek_PplPrograms_PplProgramId",
                table: "PplWeek");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PplWeek",
                table: "PplWeek");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CardioEntry",
                table: "CardioEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdditionalStrengthSet",
                table: "AdditionalStrengthSet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdditionalStrengthExercise",
                table: "AdditionalStrengthExercise");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdditionalSession",
                table: "AdditionalSession");

            migrationBuilder.RenameTable(
                name: "PplWeek",
                newName: "PplWeeks");

            migrationBuilder.RenameTable(
                name: "CardioEntry",
                newName: "CardioEntries");

            migrationBuilder.RenameTable(
                name: "AdditionalStrengthSet",
                newName: "AdditionalStrengthSets");

            migrationBuilder.RenameTable(
                name: "AdditionalStrengthExercise",
                newName: "AdditionalStrengthExercises");

            migrationBuilder.RenameTable(
                name: "AdditionalSession",
                newName: "AdditionalSessions");

            migrationBuilder.RenameIndex(
                name: "IX_PplWeek_PplProgramId_WeekNumber",
                table: "PplWeeks",
                newName: "IX_PplWeeks_PplProgramId_WeekNumber");

            migrationBuilder.RenameIndex(
                name: "IX_CardioEntry_AdditionalSessionId",
                table: "CardioEntries",
                newName: "IX_CardioEntries_AdditionalSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_CardioEntry_AccessoryId",
                table: "CardioEntries",
                newName: "IX_CardioEntries_AccessoryId");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalStrengthSet_AdditionalStrengthExerciseId",
                table: "AdditionalStrengthSets",
                newName: "IX_AdditionalStrengthSets_AdditionalStrengthExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalStrengthExercise_AdditionalSessionId",
                table: "AdditionalStrengthExercises",
                newName: "IX_AdditionalStrengthExercises_AdditionalSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalSession_WeekId_OccurredOn",
                table: "AdditionalSessions",
                newName: "IX_AdditionalSessions_WeekId_OccurredOn");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalSession_PplWeekId_OccurredOn",
                table: "AdditionalSessions",
                newName: "IX_AdditionalSessions_PplWeekId_OccurredOn");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutAccessories",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PplWeeks",
                table: "PplWeeks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CardioEntries",
                table: "CardioEntries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdditionalStrengthSets",
                table: "AdditionalStrengthSets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdditionalStrengthExercises",
                table: "AdditionalStrengthExercises",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdditionalSessions",
                table: "AdditionalSessions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalSessions_PplWeeks_PplWeekId",
                table: "AdditionalSessions",
                column: "PplWeekId",
                principalTable: "PplWeeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalSessions_Weeks_WeekId",
                table: "AdditionalSessions",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalStrengthExercises_AdditionalSessions_AdditionalSe~",
                table: "AdditionalStrengthExercises",
                column: "AdditionalSessionId",
                principalTable: "AdditionalSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalStrengthSets_AdditionalStrengthExercises_Addition~",
                table: "AdditionalStrengthSets",
                column: "AdditionalStrengthExerciseId",
                principalTable: "AdditionalStrengthExercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CardioEntries_Accessories_AccessoryId",
                table: "CardioEntries",
                column: "AccessoryId",
                principalTable: "Accessories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CardioEntries_AdditionalSessions_AdditionalSessionId",
                table: "CardioEntries",
                column: "AdditionalSessionId",
                principalTable: "AdditionalSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PplSessions_PplWeeks_PplWeekId",
                table: "PplSessions",
                column: "PplWeekId",
                principalTable: "PplWeeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PplWeeks_PplPrograms_PplProgramId",
                table: "PplWeeks",
                column: "PplProgramId",
                principalTable: "PplPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalSessions_PplWeeks_PplWeekId",
                table: "AdditionalSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalSessions_Weeks_WeekId",
                table: "AdditionalSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalStrengthExercises_AdditionalSessions_AdditionalSe~",
                table: "AdditionalStrengthExercises");

            migrationBuilder.DropForeignKey(
                name: "FK_AdditionalStrengthSets_AdditionalStrengthExercises_Addition~",
                table: "AdditionalStrengthSets");

            migrationBuilder.DropForeignKey(
                name: "FK_CardioEntries_Accessories_AccessoryId",
                table: "CardioEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_CardioEntries_AdditionalSessions_AdditionalSessionId",
                table: "CardioEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PplSessions_PplWeeks_PplWeekId",
                table: "PplSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PplWeeks_PplPrograms_PplProgramId",
                table: "PplWeeks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PplWeeks",
                table: "PplWeeks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CardioEntries",
                table: "CardioEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdditionalStrengthSets",
                table: "AdditionalStrengthSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdditionalStrengthExercises",
                table: "AdditionalStrengthExercises");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdditionalSessions",
                table: "AdditionalSessions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutAccessories");

            migrationBuilder.RenameTable(
                name: "PplWeeks",
                newName: "PplWeek");

            migrationBuilder.RenameTable(
                name: "CardioEntries",
                newName: "CardioEntry");

            migrationBuilder.RenameTable(
                name: "AdditionalStrengthSets",
                newName: "AdditionalStrengthSet");

            migrationBuilder.RenameTable(
                name: "AdditionalStrengthExercises",
                newName: "AdditionalStrengthExercise");

            migrationBuilder.RenameTable(
                name: "AdditionalSessions",
                newName: "AdditionalSession");

            migrationBuilder.RenameIndex(
                name: "IX_PplWeeks_PplProgramId_WeekNumber",
                table: "PplWeek",
                newName: "IX_PplWeek_PplProgramId_WeekNumber");

            migrationBuilder.RenameIndex(
                name: "IX_CardioEntries_AdditionalSessionId",
                table: "CardioEntry",
                newName: "IX_CardioEntry_AdditionalSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_CardioEntries_AccessoryId",
                table: "CardioEntry",
                newName: "IX_CardioEntry_AccessoryId");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalStrengthSets_AdditionalStrengthExerciseId",
                table: "AdditionalStrengthSet",
                newName: "IX_AdditionalStrengthSet_AdditionalStrengthExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalStrengthExercises_AdditionalSessionId",
                table: "AdditionalStrengthExercise",
                newName: "IX_AdditionalStrengthExercise_AdditionalSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalSessions_WeekId_OccurredOn",
                table: "AdditionalSession",
                newName: "IX_AdditionalSession_WeekId_OccurredOn");

            migrationBuilder.RenameIndex(
                name: "IX_AdditionalSessions_PplWeekId_OccurredOn",
                table: "AdditionalSession",
                newName: "IX_AdditionalSession_PplWeekId_OccurredOn");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PplWeek",
                table: "PplWeek",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CardioEntry",
                table: "CardioEntry",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdditionalStrengthSet",
                table: "AdditionalStrengthSet",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdditionalStrengthExercise",
                table: "AdditionalStrengthExercise",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdditionalSession",
                table: "AdditionalSession",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalSession_PplWeek_PplWeekId",
                table: "AdditionalSession",
                column: "PplWeekId",
                principalTable: "PplWeek",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalSession_Weeks_WeekId",
                table: "AdditionalSession",
                column: "WeekId",
                principalTable: "Weeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalStrengthExercise_AdditionalSession_AdditionalSess~",
                table: "AdditionalStrengthExercise",
                column: "AdditionalSessionId",
                principalTable: "AdditionalSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalStrengthSet_AdditionalStrengthExercise_Additional~",
                table: "AdditionalStrengthSet",
                column: "AdditionalStrengthExerciseId",
                principalTable: "AdditionalStrengthExercise",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CardioEntry_Accessories_AccessoryId",
                table: "CardioEntry",
                column: "AccessoryId",
                principalTable: "Accessories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CardioEntry_AdditionalSession_AdditionalSessionId",
                table: "CardioEntry",
                column: "AdditionalSessionId",
                principalTable: "AdditionalSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PplSessions_PplWeek_PplWeekId",
                table: "PplSessions",
                column: "PplWeekId",
                principalTable: "PplWeek",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PplWeek_PplPrograms_PplProgramId",
                table: "PplWeek",
                column: "PplProgramId",
                principalTable: "PplPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
