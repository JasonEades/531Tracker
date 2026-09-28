using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressPhotoUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ProgressPhotos",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            // Existing photos have never been edited, so their version matches their upload time.
            migrationBuilder.Sql(@"UPDATE ""ProgressPhotos"" SET ""UpdatedAtUtc"" = ""CreatedAtUtc"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ProgressPhotos");
        }
    }
}
