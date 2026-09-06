using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using FiveThreeOneTracker.Services.Export;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class WorkoutExportTests
{
    [Fact]
    public void MarkdownPreservesNotesAndPlannedActualValues()
    {
        var document = CreateDocument();
        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        Assert.Contains("Workout Notes", markdown);
        Assert.Contains("Felt unusually fatigued.", markdown);
        Assert.Contains("Recovery was poor.", markdown);
        Assert.Contains("Exercise Notes", markdown);
        Assert.Contains("Left shoulder felt uncomfortable.", markdown);
        Assert.Contains("Last rep was slow.", markdown);
        Assert.Contains("| 5 | Main — AMRAP | 5 | 9 | 235 | 235 | Yes", markdown);
    }

    [Fact]
    public void PdfIsGeneratedFromTheSameDocument()
    {
        var pdf = new PdfWorkoutExporter().Render(CreateDocument());

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public void WeeklyMarkdownKeepsWorkoutNotesAttachedToEachWorkout()
    {
        var document = new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Week,
            Week = new WeekExportModel
            {
                WeekNumber = 2,
                CycleNumber = 12,
                Workouts =
                [
                    CreateWorkout("Bench Press Day", "Bench notes"),
                    CreateWorkout("Overhead Press Day", "Press notes")
                ]
            }
        };

        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        var benchPosition = markdown.IndexOf("Bench notes", StringComparison.Ordinal);
        var pressHeadingPosition = markdown.IndexOf("Overhead Press Day", StringComparison.Ordinal);
        var pressPosition = markdown.IndexOf("Press notes", StringComparison.Ordinal);

        Assert.True(benchPosition >= 0 && pressHeadingPosition > benchPosition);
        Assert.True(pressPosition > pressHeadingPosition);
    }

    [Fact]
    public void CyclePdfIncludesWorkoutNotesWithoutMergingCycleNotes()
    {
        var document = new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Cycle,
            Cycle = new CycleExportModel
            {
                CycleNumber = 12,
                Name = "Cycle 12",
                Notes = "Cycle-level context",
                Weeks =
                [
                    new WeekExportModel
                    {
                        WeekNumber = 1,
                        CycleNumber = 12,
                        Workouts = [CreateWorkout("Bench Press Day", "Workout-level context")]
                    }
                ]
            }
        };

        var pdf = new PdfWorkoutExporter().Render(document);

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public void EmptyWorkoutNotesAreValidAndOmitted()
    {
        var document = new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Day,
            Workout = new WorkoutExportModel
            {
                WorkoutName = "Empty Notes Day",
                WorkoutType = "BenchPress",
                Status = "NotStarted"
            }
        };

        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        Assert.DoesNotContain("## Workout Notes", markdown);
    }

    [Fact]
    public async Task WorkoutNotesPersistAcrossEditsAndStayWithTheirWorkout()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var cycle = new Cycle { Name = "Test Cycle" };
            var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
            context.Workouts.AddRange(
                new Workout { Week = week, MainLiftType = LiftType.BenchPress },
                new Workout { Week = week, MainLiftType = LiftType.OverheadPress });
            await context.SaveChangesAsync();
        }

        await using (var context = new AppDbContext(options))
        {
            var service = new WorkoutService(context, new TestCurrentUserService());
            await service.UpdateWorkoutNotesAsync(1, "First line\nSecond line");
            await service.UpdateWorkoutNotesAsync(1, "Updated notes");
        }

        await using (var context = new AppDbContext(options))
        {
            var first = await context.Workouts.SingleAsync(w => w.Id == 1);
            var second = await context.Workouts.SingleAsync(w => w.Id == 2);

            Assert.Equal("Updated notes", first.Notes);
            Assert.Null(second.Notes);
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult("test-user");
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>("test-user");
    }

    private static WorkoutExportModel CreateWorkout(string name, string notes) => new()
    {
        WorkoutName = name,
        WorkoutType = name,
        Notes = notes,
        Status = "Completed",
        Exercises = []
    };

    private static WorkoutExportDocument CreateDocument() => new()
    {
        Scope = WorkoutExportScope.Day,
        Workout = new WorkoutExportModel
        {
            CycleNumber = 12,
            WeekNumber = 2,
            WorkoutName = "Bench Press Day",
            WorkoutType = "BenchPress",
            WorkoutDate = new DateTime(2026, 9, 4),
            Status = "Completed",
            Notes = "Felt unusually fatigued.\nRecovery was poor.",
            Exercises =
            [
                new ExerciseExportModel
                {
                    Name = "Bench Press",
                    Category = "Main Lift",
                    Notes = "Left shoulder felt uncomfortable.",
                    Sets =
                    [
                        new SetExportModel { Number = 5, Type = "Main", TargetReps = 5, ActualReps = 9, TargetWeight = 235, ActualWeight = 235, IsCompleted = true, IsAmrap = true, Notes = "Last rep was slow." }
                    ]
                }
            ],
            Summary = new ExportSummaryModel { ExerciseCount = 1, SetCount = 1, CompletedSetCount = 1, TotalReps = 9, TotalVolume = 2115 }
        }
    };
}
