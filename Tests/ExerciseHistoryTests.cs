using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WorkoutExportTests;

public sealed class ExerciseHistoryTests
{
    [Theory]
    [InlineData("Bench Press", "bench-press")]
    [InlineData("  Incline DB Press  ", "incline-db-press")]
    [InlineData("Face Pull (cable)", "face-pull-cable")]
    public void KeysAreUrlSafeAndStable(string name, string expected)
    {
        Assert.Equal(expected, ExerciseHistoryService.BuildKey(name));
    }

    [Fact]
    public async Task HistoryMergesSourcesByDayAndComparesAgainstThePreviousSession()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var bench = new Lift { UserId = "test-user", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 200 };
        var cycle = new Cycle { UserId = "test-user", Name = "Cycle 1", CycleNumber = 1 };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var today = DateTime.UtcNow.Date;

        var older = new Workout
        {
            Week = week,
            MainLiftType = LiftType.BenchPress,
            Status = WorkoutStatus.Completed,
            OccurredOn = today.AddDays(-14),
            Sets =
            [
                Completed(bench, SetType.Warmup, 1, 95, 5),
                Completed(bench, SetType.Main, 2, 160, 5)
            ]
        };

        var recent = new Workout
        {
            Week = week,
            MainLiftType = LiftType.BenchPress,
            Status = WorkoutStatus.Completed,
            OccurredOn = today.AddDays(-7),
            Sets = [Completed(bench, SetType.Main, 1, 170, 5)]
        };

        context.Cycles.Add(cycle);
        context.Workouts.AddRange(older, recent);
        await context.SaveChangesAsync();

        var service = new ExerciseHistoryService(context, new TestCurrentUserService(), new WeightCalculator());
        var history = await service.GetExerciseHistoryAsync("bench-press");

        Assert.NotNull(history);
        Assert.Equal("Bench Press", history!.Name);
        Assert.Equal(2, history.Sessions.Count);

        // Most recent first, and warmups never appear.
        Assert.Equal(today.AddDays(-7), history.Sessions[0].Date);
        Assert.All(history.Sessions.SelectMany(x => x.Sets), set => Assert.True(set.Weight >= 160));

        Assert.Equal(200, history.Best1Rm);
        Assert.Equal(170, history.HeaviestWeight);
        Assert.True(history.Sessions[0].IsBest1Rm);

        // 170x5 -> 200 e1RM vs 160x5 -> 185 e1RM.
        Assert.Equal(15, history.Sessions[0].Change1RmVsPrevious);
        Assert.Equal(50, history.Sessions[0].ChangeVolumeVsPrevious);
    }

    [Fact]
    public async Task SummariesExcludeOtherUsersAndReportLastPerformed()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var today = DateTime.UtcNow.Date;

        var mine = new Lift { UserId = "test-user", LiftType = LiftType.Squat, Name = "Squat", TrainingMax = 300 };
        var myCycle = new Cycle { UserId = "test-user", Name = "Mine", CycleNumber = 1 };
        context.Cycles.Add(myCycle);
        context.Workouts.Add(new Workout
        {
            Week = new Week { Cycle = myCycle, WeekNumber = WeekNumber.Week1 },
            MainLiftType = LiftType.Squat,
            Status = WorkoutStatus.Completed,
            OccurredOn = today.AddDays(-3),
            Sets = [Completed(mine, SetType.Main, 1, 250, 5)]
        });

        var theirs = new Lift { UserId = "other-user", LiftType = LiftType.Deadlift, Name = "Deadlift", TrainingMax = 400 };
        var theirCycle = new Cycle { UserId = "other-user", Name = "Theirs", CycleNumber = 1 };
        context.Cycles.Add(theirCycle);
        context.Workouts.Add(new Workout
        {
            Week = new Week { Cycle = theirCycle, WeekNumber = WeekNumber.Week1 },
            MainLiftType = LiftType.Deadlift,
            Status = WorkoutStatus.Completed,
            OccurredOn = today.AddDays(-1),
            Sets = [Completed(theirs, SetType.Main, 1, 405, 1)]
        });

        await context.SaveChangesAsync();

        var service = new ExerciseHistoryService(context, new TestCurrentUserService(), new WeightCalculator());
        var summaries = await service.GetExerciseSummariesAsync();

        var summary = Assert.Single(summaries);
        Assert.Equal("Squat", summary.Name);
        Assert.Equal("squat", summary.Key);
        Assert.Equal(today.AddDays(-3), summary.LastPerformed);
        Assert.Equal(1, summary.SessionCount);
    }

    [Fact]
    public async Task UnknownExerciseKeyReturnsNull()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var service = new ExerciseHistoryService(context, new TestCurrentUserService(), new WeightCalculator());

        Assert.Null(await service.GetExerciseHistoryAsync("nothing-here"));
        Assert.Null(await service.GetExerciseHistoryAsync(""));
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
