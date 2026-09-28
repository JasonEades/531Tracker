using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutExportTests;

public sealed class DashboardAnalyticsTests
{
    [Theory]
    [InlineData(225, 5, 260)]
    [InlineData(275, 1, 275)]
    [InlineData(100, 10, 135)]
    public void EstimatedOneRepMaxUsesExistingEpleyFormula(double weight, int reps, double expected)
    {
        var calculator = new WeightCalculator();

        Assert.Equal(expected, calculator.CalculateEstimated1RM(weight, reps));
    }

    [Fact]
    public void CycleCompletionUsesProgrammedSessionsOnly()
    {
        var analytics = new CurrentProgramAnalytics
        {
            PlannedSessions = 16,
            CompletedSessions = 10,
            AdditionalSessions = 8
        };

        Assert.Equal(63, analytics.CompletionPercent);
        Assert.Equal(8, analytics.AdditionalSessions);
    }

    [Fact]
    public void EmptyCycleHasZeroCompletionInsteadOfFabricatingProgress()
    {
        var analytics = new CurrentProgramAnalytics();

        Assert.Equal(0, analytics.CompletionPercent);
    }

    [Fact]
    public void ConnectedStepAnalyticsProvidesThirtyDays()
    {
        var analytics = new StepAnalytics
        {
            IsConnected = true,
            Last30Days = Enumerable.Range(0, 30)
                .Select(i => new DailyStepPoint { Date = DateTime.UtcNow.Date.AddDays(i - 29), Steps = i * 1000 })
                .ToList()
        };

        Assert.True(analytics.IsConnected);
        Assert.Equal(30, analytics.Last30Days.Count);
        Assert.Equal(29_000, analytics.Last30Days[^1].Steps);
    }

    [Fact]
    public async Task StrengthProgressUsesMainWorkOfTheOwningLiftAndIgnoresDeloadAndAssistance()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var bench = new Lift { UserId = "test-user", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 200 };
        var squat = new Lift { UserId = "test-user", LiftType = LiftType.Squat, Name = "Squat", TrainingMax = 300 };
        var cycle = new Cycle { UserId = "test-user", Name = "Cycle 1", CycleNumber = 1, BbbMode = BbbMode.OppositeDay };
        var week1 = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var week4 = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week4 };
        var today = DateTime.UtcNow.Date;

        // Week 1 bench day: real top set plus warmup and opposite-day squat BBB assistance.
        var heavyDay = new Workout
        {
            Week = week1,
            MainLiftType = LiftType.BenchPress,
            Status = WorkoutStatus.Completed,
            OccurredOn = today.AddDays(-21),
            Sets =
            [
                Completed(bench, SetType.Warmup, 1, 80, 5),
                Completed(bench, SetType.Main, 2, 170, 5),
                Completed(squat, SetType.Bbb, 3, 165, 10)
            ]
        };

        // Most recent bench day is a deload, which must not define current strength.
        var deloadDay = new Workout
        {
            Week = week4,
            MainLiftType = LiftType.BenchPress,
            Status = WorkoutStatus.Completed,
            OccurredOn = today.AddDays(-2),
            Sets = [Completed(bench, SetType.Main, 1, 120, 5)]
        };

        context.Cycles.Add(cycle);
        context.Workouts.AddRange(heavyDay, deloadDay);
        await context.SaveChangesAsync();

        var service = new DashboardAnalyticsService(context, new TestCurrentUserService(), new WeightCalculator());
        var analytics = await service.GetDashboardAsync();

        var entry = Assert.Single(analytics.StrengthProgress);
        Assert.Equal("Bench Press", entry.ExerciseName);
        Assert.Equal(200, entry.Current);
        Assert.True(entry.Current >= bench.TrainingMax);
    }

    private static WorkoutSet Completed(Lift lift, SetType setType, int setNumber, double weight, int reps) => new()
    {
        Lift = lift,
        SetType = setType,
        SetNumber = setNumber,
        PrescribedWeight = weight,
        PrescribedReps = reps,
        ActualWeight = weight,
        ActualReps = reps,
        IsCompleted = true
    };

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult("test-user");
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>("test-user");
    }
}
