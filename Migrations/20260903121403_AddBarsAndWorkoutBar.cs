using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddBarsAndWorkoutBar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bars", x => x.Id);
                });

            // Data migration: create a default Bar row for each existing user from their
            // current UserEquipment.BarWeight before that column is dropped.
            migrationBuilder.Sql(@"
                INSERT INTO ""Bars"" (""UserId"", ""Name"", ""Weight"", ""IsDefault"")
                SELECT ""UserId"", 'Standard Olympic', ""BarWeight"", true
                FROM ""UserEquipment""
                WHERE ""UserId"" IS NOT NULL;
            ");

            migrationBuilder.DropColumn(
                name: "BarWeight",
                table: "UserEquipment");

            migrationBuilder.AddColumn<int>(
                name: "BarId",
                table: "Workouts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_BarId",
                table: "Workouts",
                column: "BarId");

            migrationBuilder.CreateIndex(
                name: "IX_Bars_UserId",
                table: "Bars",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workouts_Bars_BarId",
                table: "Workouts",
                column: "BarId",
                principalTable: "Bars",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workouts_Bars_BarId",
                table: "Workouts");

            migrationBuilder.DropTable(
                name: "Bars");

            migrationBuilder.DropIndex(
                name: "IX_Workouts_BarId",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "BarId",
                table: "Workouts");

            migrationBuilder.AddColumn<double>(
                name: "BarWeight",
                table: "UserEquipment",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
