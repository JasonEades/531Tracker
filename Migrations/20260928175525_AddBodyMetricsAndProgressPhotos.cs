using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FiveThreeOneTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyMetricsAndProgressPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BodyMetricEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RecordedOn = table.Column<DateTime>(type: "date", nullable: false),
                    WeightLb = table.Column<double>(type: "double precision", nullable: true),
                    BodyFatPercent = table.Column<double>(type: "double precision", nullable: true),
                    NeckInches = table.Column<double>(type: "double precision", nullable: true),
                    ChestInches = table.Column<double>(type: "double precision", nullable: true),
                    WaistInches = table.Column<double>(type: "double precision", nullable: true),
                    HipsInches = table.Column<double>(type: "double precision", nullable: true),
                    ThighInches = table.Column<double>(type: "double precision", nullable: true),
                    ArmInches = table.Column<double>(type: "double precision", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyMetricEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodyMetricEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TakenOn = table.Column<DateTime>(type: "date", nullable: false),
                    ImageData = table.Column<byte[]>(type: "bytea", nullable: false),
                    ThumbnailData = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ThumbnailSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressPhotos_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BodyMetricEntries_UserId_RecordedOn",
                table: "BodyMetricEntries",
                columns: new[] { "UserId", "RecordedOn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgressPhotos_UserId_TakenOn",
                table: "ProgressPhotos",
                columns: new[] { "UserId", "TakenOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodyMetricEntries");

            migrationBuilder.DropTable(
                name: "ProgressPhotos");
        }
    }
}
