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

    [Theory]
    [InlineData("Barbell Back Squat", LiftType.Squat)]
    [InlineData("Squats", LiftType.Squat)]
    [InlineData("BB Bench Press", LiftType.BenchPress)]
    [InlineData("OHP", LiftType.OverheadPress)]
    [InlineData("Barbell Deadlift", LiftType.Deadlift)]
    [InlineData("Front Squat", null)]
    [InlineData("Romanian Deadlift", null)]
    [InlineData("Incline Barbell Bench", null)]
    [InlineData("Dumbbell Bench Press", null)]
    [InlineData("Trap Bar Deadlift", null)]
    [InlineData("Leg Press", null)]
    public void CompoundLiftResolutionAcceptsNamingVariantsButNotDifferentMovements(string name, LiftType? expected)
    {
        Assert.Equal(expected, DashboardAnalyticsService.ResolveCompoundLift(name));
    }

    [Fact]
    public async Task CompoundProgressSpansAllTimeFromFirstSessionToCurrentAcrossProgramsForCurrentUserOnly()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var today = DateTime.UtcNow.Date;
        var bench = new Lift { UserId = "test-user", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 200 };
        var cycle = new Cycle { UserId = "test-user", Name = "Cycle 1", CycleNumber = 1, BbbMode = BbbMode.None };
        var week1 = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var week4 = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week4 };

        // First session is older than the 1-year dashboard window but must still count as the starting point.
        context.Workouts.AddRange(
            new Workout { Week = week1, MainLiftType = LiftType.BenchPress, Status = WorkoutStatus.Completed, OccurredOn = today.AddYears(-2), Sets = [Completed(bench, SetType.Main, 1, 150, 5)] },
            new Workout { Week = week4, MainLiftType = LiftType.BenchPress, Status = WorkoutStatus.Completed, OccurredOn = today.AddYears(-2).AddDays(-7), Sets = [Completed(bench, SetType.Main, 1, 100, 5)] });

        var program = new PplProgram { UserId = "test-user", Name = "PPL" };
        var otherProgram = new PplProgram { UserId = "other-user", Name = "Other PPL" };
        context.PplSessions.AddRange(
            PplBenchSession(program, today.AddDays(-3), 185, 5),
            PplBenchSession(otherProgram, today.AddDays(-1), 400, 5));
        await context.SaveChangesAsync();

        var service = new DashboardAnalyticsService(context, new TestCurrentUserService(), new WeightCalculator());
        var analytics = await service.GetDashboardAsync();

        var benchProgress = Assert.Single(analytics.CompoundProgress);
        Assert.Equal(LiftType.BenchPress, benchProgress.LiftType);
        Assert.Equal(175, benchProgress.First.Value);
        Assert.Equal(today.AddYears(-2), benchProgress.First.Date);
        Assert.Equal(215, benchProgress.Current.Value);
        Assert.Equal(today.AddDays(-3), benchProgress.Current.Date);
        Assert.Equal(40, benchProgress.Change);
        Assert.Equal(2, benchProgress.History.Count);
    }

    private static PplSession PplBenchSession(PplProgram program, DateTime occurredOn, double weight, int reps)
    {
        var template = new PplDayTemplate { Program = program, DayType = PplDayType.Push, Name = "Push" };
        var slot = new PplExerciseSlot { DayTemplate = template, ExerciseName = "Bench Press", MuscleGroup = MuscleGroup.Chest };
        return new PplSession
        {
            Program = program,
            DayTemplate = template,
            Status = WorkoutStatus.Completed,
            OccurredOn = occurredOn,
            Exercises =
            [
                new PplSessionExercise
                {
                    ExerciseSlot = slot,
                    ExerciseName = "Bench Press",
                    Sets = [new PplSessionSet { SetNumber = 1, TargetReps = reps, ActualReps = reps, ActualWeight = weight, IsCompleted = true }]
                }
            ]
        };
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
