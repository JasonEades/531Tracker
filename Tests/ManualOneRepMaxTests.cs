using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutExportTests;

public sealed class ManualOneRepMaxTests : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private AppDbContext context = null!;

    public async Task InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        // Manual 1RM rows are FK-constrained to the identity user table.
        context.Users.AddRange(
            new ApplicationUser { Id = "test-user", UserName = "test-user", Email = "test@example.com" },
            new ApplicationUser { Id = "other-user", UserName = "other-user", Email = "other@example.com" });
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
        await connection.DisposeAsync();
    }

    private ManualOneRepMaxService Service(string userId = "test-user") =>
        new(context, new TestCurrentUserService(userId), new WeightCalculator());

    [Fact]
    public async Task SaveValidatesInputAndReusesLoggedExerciseSpelling()
    {
        var service = Service();
        Assert.False((await service.SaveAsync(0, " ", 200, DateTime.Today, null)).Success);
        Assert.False((await service.SaveAsync(0, "Bench Press", 0, DateTime.Today, null)).Success);
        Assert.False((await service.SaveAsync(0, "Bench Press", double.NaN, DateTime.Today, null)).Success);
        Assert.False((await service.SaveAsync(0, "Bench Press", 200, DateTime.Today.AddDays(5), null)).Success);

        context.Lifts.Add(new Lift { UserId = "test-user", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 200 });
        await context.SaveChangesAsync();

        var saved = await service.SaveAsync(0, "  bench-press ", 225, DateTime.Today.AddYears(-3), "  gym test ");

        Assert.True(saved.Success);
        Assert.Equal("Bench Press", saved.Entry!.ExerciseName);
        Assert.Equal("gym test", saved.Entry.Notes);
        Assert.Equal(DateTime.Today.AddYears(-3), saved.Entry.AchievedOn);
    }

    [Fact]
    public async Task EntriesAreIsolatedByUser()
    {
        var mine = (await Service().SaveAsync(0, "Squat", 315, DateTime.Today.AddYears(-1), null)).Entry!;
        var other = Service("other-user");

        Assert.Empty(await other.GetEntriesAsync());
        Assert.False((await other.SaveAsync(mine.Id, "Squat", 600, DateTime.Today, null)).Success);
        Assert.False(await other.DeleteAsync(mine.Id));
        context.ChangeTracker.Clear();

        var stored = Assert.Single(await Service().GetEntriesAsync());
        Assert.Equal(315, stored.Weight);

        Assert.True(await Service().DeleteAsync(mine.Id));
        Assert.Empty(await Service().GetEntriesAsync());
    }

    [Fact]
    public async Task ManualMaxesFeedDashboardProgressAndHistoryButNotWeightSuggestions()
    {
        var today = DateTime.UtcNow.Date;
        var program = new PplProgram { UserId = "test-user", Name = "PPL" };
        var template = new PplDayTemplate { Program = program, DayType = PplDayType.Push, Name = "Push" };
        var slot = new PplExerciseSlot { DayTemplate = template, ExerciseName = "Bench Press", MuscleGroup = MuscleGroup.Chest };
        context.PplSessions.Add(new PplSession
        {
            Program = program,
            DayTemplate = template,
            Status = WorkoutStatus.Completed,
            OccurredOn = today.AddDays(-3),
            Exercises =
            [
                new PplSessionExercise
                {
                    ExerciseSlot = slot,
                    ExerciseName = "Bench Press",
                    Sets = [new PplSessionSet { SetNumber = 1, TargetReps = 5, ActualReps = 5, ActualWeight = 185, IsCompleted = true }]
                }
            ]
        });
        await context.SaveChangesAsync();

        await Service().SaveAsync(0, "Bench Press", 205, today.AddYears(-2), null);
        await Service().SaveAsync(0, "Farmer Carry", 300, today.AddMonths(-8), null);
        await Service("other-user").SaveAsync(0, "Bench Press", 500, today.AddYears(-3), null);

        var analytics = await new DashboardAnalyticsService(context, new TestCurrentUserService("test-user"), new WeightCalculator())
            .GetDashboardAsync();

        var bench = Assert.Single(analytics.CompoundProgress);
        Assert.Equal(205, bench.First.Value);
        Assert.Equal(today.AddYears(-2), bench.First.Date);
        Assert.Equal(215, bench.Current.Value);

        var benchRow = analytics.StrengthProgress.Single(s => s.ExerciseName == "Bench Press");
        Assert.Equal(215, benchRow.Current);
        Assert.Equal(205, benchRow.OneYearAgo);
        var carryRow = analytics.StrengthProgress.Single(s => s.ExerciseName == "Farmer Carry");
        Assert.Equal(300, carryRow.SixMonthsAgo);

        Assert.Contains(analytics.PersonalRecords, r => r.ExerciseName == "Farmer Carry" && r.RecordType == "Heaviest weight" && r.Value == 300);
        Assert.DoesNotContain(analytics.PersonalRecords, r => r.ExerciseName == "Farmer Carry" && r.RecordType == "Rep PR");
        Assert.DoesNotContain(analytics.PersonalRecords, r => r.Value == 500);

        var history = await new ExerciseHistoryService(context, new TestCurrentUserService("test-user"), new WeightCalculator())
            .GetExerciseHistoryAsync("bench-press");
        Assert.Contains(history!.Sessions, s => s.Source == "Manual" && s.Sets.Single().Weight == 205);
        Assert.DoesNotContain(history.Sessions, s => s.Sets.Any(x => x.Weight == 500));

        // Working-weight suggestions and "last time" lookups never see manual maxes.
        var suggestionSource = await ExerciseObservations.LoadAsync(context, "test-user", new WeightCalculator());
        Assert.DoesNotContain(suggestionSource, o => o.Kind == ExerciseObservationKind.Manual);
    }

    private sealed class TestCurrentUserService(string userId) : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult(userId);
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>(userId);
    }
}
