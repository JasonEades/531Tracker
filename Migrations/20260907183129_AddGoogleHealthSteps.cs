using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleHealthSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Cycles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyStepRecordId",
                table: "CardioEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CardioEntries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyStepRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LocalDate = table.Column<DateTime>(type: "date", nullable: false),
                    StepCount = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Metric = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderRecordId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyStepRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyStepRecords_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleHealthConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GoogleSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "text", nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "text", nullable: true),
                    AccessTokenExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedScopes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ConnectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleHealthConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleHealthConnections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Accessories",
                columns: new[] { "Id", "Category", "Description", "IsActive", "Name", "UserId" },
                values: new object[] { 26, "Cardio", null, true, "Steps", null });

            migrationBuilder.CreateIndex(
                name: "IX_Cycles_UserId_StartDate",
                table: "Cycles",
                columns: new[] { "UserId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CardioEntries_DailyStepRecordId",
                table: "CardioEntries",
                column: "DailyStepRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyStepRecords_UserId_Provider_Metric_LocalDate",
                table: "DailyStepRecords",
                columns: new[] { "UserId", "Provider", "Metric", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleHealthConnections_UserId",
                table: "GoogleHealthConnections",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CardioEntries_DailyStepRecords_DailyStepRecordId",
                table: "CardioEntries",
                column: "DailyStepRecordId",
                principalTable: "DailyStepRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardioEntries_DailyStepRecords_DailyStepRecordId",
                table: "CardioEntries");

            migrationBuilder.DropTable(
                name: "DailyStepRecords");

            migrationBuilder.DropTable(
                name: "GoogleHealthConnections");

            migrationBuilder.DropIndex(
                name: "IX_Cycles_UserId_StartDate",
                table: "Cycles");

            migrationBuilder.DropIndex(
                name: "IX_CardioEntries_DailyStepRecordId",
                table: "CardioEntries");

            migrationBuilder.DeleteData(
                table: "Accessories",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Cycles");

            migrationBuilder.DropColumn(
                name: "DailyStepRecordId",
                table: "CardioEntries");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CardioEntries");
        }
    }
}
