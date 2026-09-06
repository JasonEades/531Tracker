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
        var document = CreateDocument();
        document.Workout!.Exercises[0].AdditionalSets.Add(new SetExportModel
        {
            Number = 1,
            Type = "DropSet",
            ActualWeight = 165,
            ActualReps = 12,
            Rpe = 8,
            Notes = "After AMRAP",
            IsCompleted = true
        });

        var pdf = new PdfWorkoutExporter().Render(document);

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
    public void MarkdownSeparatesAdditionalSetsFromProgrammedSets()
    {
        var document = CreateDocument();
        document.Workout!.Exercises[0].AdditionalSets.Add(new SetExportModel
        {
            Number = 1,
            Type = "DropSet",
            ActualWeight = 165,
            ActualReps = 12,
            Rpe = 8,
            Notes = "Drop set after AMRAP",
            IsCompleted = true
        });

        var markdown = System.Text.Encoding.UTF8.GetString(new MarkdownWorkoutExporter().Render(document));

        Assert.Contains("### Additional Sets", markdown);
        Assert.Contains("| 1 | DropSet | 165 | 12 | 8 |", markdown);
        Assert.True(markdown.IndexOf("### Additional Sets", StringComparison.Ordinal) > markdown.IndexOf("| 5 | Main", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdditionalSetsPersistInOrderWithoutChangingProgrammedSets()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var lift = new Lift { LiftType = LiftType.BenchPress, Name = "Bench Press", UserId = "test-user" };
            var cycle = new Cycle { Name = "Test Cycle", UserId = "test-user" };
            var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
            var workout = new Workout { Week = week, MainLiftType = LiftType.BenchPress };
            workout.Sets.Add(new WorkoutSet { Lift = lift, SetType = SetType.Main, SetNumber = 1, PrescribedWeight = 185, PrescribedReps = 5 });
            context.Add(workout);
            await context.SaveChangesAsync();
        }

        int firstId;
        await using (var context = new AppDbContext(options))
        {
            var service = new WorkoutService(context, new TestCurrentUserService());
            var first = await service.AddAdditionalSetAsync(1, 165, 12, AdditionalSetType.DropSet, 8, null, "After top set");
            var second = await service.AddAdditionalSetAsync(1, 145, 12, AdditionalSetType.DropSet, 9, null, null);
            Assert.NotNull(first);
            Assert.NotNull(second);
            firstId = first!.Id;
            Assert.Equal(1, first.Sequence);
            Assert.Equal(2, second!.Sequence);
        }

        await using (var context = new AppDbContext(options))
        {
            var programmed = await context.WorkoutSets.SingleAsync(s => !s.IsAdditional);
            var additional = await context.WorkoutSets.Where(s => s.IsAdditional).OrderBy(s => s.Sequence).ToListAsync();
            Assert.Equal(185, programmed.PrescribedWeight);
            Assert.Equal(5, programmed.PrescribedReps);
            Assert.Equal(2, additional.Count);
            Assert.Equal("After top set", additional[0].Notes);

            var service = new WorkoutService(context, new TestCurrentUserService());
            await service.UpdateAdditionalSetAsync(firstId, 160, 10, AdditionalSetType.BackOffSet, 7, 2, "Updated");
            await service.DeleteAdditionalSetAsync(additional[1].Id);
        }

        await using (var context = new AppDbContext(options))
        {
            var remaining = await context.WorkoutSets.Where(s => s.IsAdditional).ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(160, remaining[0].ActualWeight);
            Assert.Equal(AdditionalSetType.BackOffSet, remaining[0].AdditionalSetType);
            Assert.Equal("Updated", remaining[0].Notes);
            Assert.Single(await context.WorkoutSets.Where(s => !s.IsAdditional).ToListAsync());
        }
    }

    [Fact]
    public async Task AdditionalSetValidationRejectsInvalidValuesAndNonPrimaryLifts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var context = new AppDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var bench = new Lift { LiftType = LiftType.BenchPress, Name = "Bench Press", UserId = "test-user" };
            var deadlift = new Lift { LiftType = LiftType.Deadlift, Name = "Deadlift", UserId = "test-user" };
            var cycle = new Cycle { Name = "Test Cycle", UserId = "test-user" };
            var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
            var workout = new Workout { Week = week, MainLiftType = LiftType.BenchPress };
            workout.Sets.Add(new WorkoutSet { Lift = deadlift, SetType = SetType.Main, SetNumber = 1, PrescribedWeight = 315, PrescribedReps = 5 });
            context.AddRange(bench, workout);
            await context.SaveChangesAsync();
        }

        await using (var context = new AppDbContext(options))
        {
            var service = new WorkoutService(context, new TestCurrentUserService());

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.AddAdditionalSetAsync(1, -1, 5, AdditionalSetType.Additional, null, null, null));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.AddAdditionalSetAsync(1, 100, 0, AdditionalSetType.Additional, null, null, null));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.AddAdditionalSetAsync(1, 100, 5, AdditionalSetType.Additional, 11, null, null));

            var result = await service.AddAdditionalSetAsync(1, 100, 5, AdditionalSetType.Additional, null, null, null);
            Assert.Null(result);
            Assert.Empty(await context.WorkoutSets.Where(s => s.IsAdditional).ToListAsync());
        }
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
