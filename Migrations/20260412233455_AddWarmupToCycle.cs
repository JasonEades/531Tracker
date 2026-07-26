using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddWarmupToCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeWarmup",
                table: "Cycles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeWarmup",
                table: "Cycles");
        }
    }
}
