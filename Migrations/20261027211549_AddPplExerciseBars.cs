using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPplExerciseBars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BarId",
                table: "PplSessionExercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BarId",
                table: "PplExerciseSlots",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PplSessionExercises_BarId",
                table: "PplSessionExercises",
                column: "BarId");

            migrationBuilder.CreateIndex(
                name: "IX_PplExerciseSlots_BarId",
                table: "PplExerciseSlots",
                column: "BarId");

            migrationBuilder.AddForeignKey(
                name: "FK_PplExerciseSlots_Bars_BarId",
                table: "PplExerciseSlots",
                column: "BarId",
                principalTable: "Bars",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PplSessionExercises_Bars_BarId",
                table: "PplSessionExercises",
                column: "BarId",
                principalTable: "Bars",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PplExerciseSlots_Bars_BarId",
                table: "PplExerciseSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_PplSessionExercises_Bars_BarId",
                table: "PplSessionExercises");

            migrationBuilder.DropIndex(
                name: "IX_PplSessionExercises_BarId",
                table: "PplSessionExercises");

            migrationBuilder.DropIndex(
                name: "IX_PplExerciseSlots_BarId",
                table: "PplExerciseSlots");

            migrationBuilder.DropColumn(
                name: "BarId",
                table: "PplSessionExercises");

            migrationBuilder.DropColumn(
                name: "BarId",
                table: "PplExerciseSlots");
        }
    }
}
