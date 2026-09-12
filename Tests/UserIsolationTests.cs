using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class UserIsolationTests
{
    [Fact]
    public async Task WorkoutReadAndMutationsRejectAnotherUsersWorkout()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycle = new Cycle { UserId = "owner", Name = "Owner Cycle" };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var lift = new Lift { UserId = "owner", LiftType = LiftType.BenchPress, Name = "Bench Press", TrainingMax = 200 };
        var workout = new Workout
        {
            Week = week,
            MainLiftType = LiftType.BenchPress,
            Status = WorkoutStatus.NotStarted,
            Sets = [new WorkoutSet
            {
                Lift = lift,
                SetType = SetType.Main,
                SetNumber = 1,
                PrescribedWeight = 100,
                PrescribedReps = 5
            }]
        };
        context.Cycles.Add(cycle);
        context.Workouts.Add(workout);
        await context.SaveChangesAsync();

        var service = new WorkoutService(context, new TestCurrentUserService("other"));

        Assert.Null(await service.GetWorkoutWithDetailsAsync(workout.Id));

        await service.UpdateSetAsync(workout.Sets.Single().Id, 125, 8, true);
        await service.UpdateWorkoutNotesAsync(workout.Id, "Should not be saved");
        await service.CompleteWorkoutAsync(workout.Id);

        var unchanged = await context.Workouts
            .Include(w => w.Sets)
            .SingleAsync(w => w.Id == workout.Id);
        Assert.Equal(WorkoutStatus.NotStarted, unchanged.Status);
        Assert.Null(unchanged.Notes);
        Assert.Null(unchanged.Sets.Single().ActualWeight);
        Assert.Null(unchanged.Sets.Single().ActualReps);
        Assert.False(unchanged.Sets.Single().IsCompleted);
    }

    [Fact]
    public async Task AdditionalSessionCreationRejectsAnotherUsersCycle()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var cycle = new Cycle { UserId = "owner", Name = "Owner Cycle" };
        context.Cycles.Add(cycle);
        await context.SaveChangesAsync();

        var service = new AdditionalSessionService(context, new TestCurrentUserService("other"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateForCycleAsync(cycle.Id, SessionType.Cardio, DateTime.UtcNow, "Hidden", null));
        Assert.Empty(await context.AdditionalSessions.ToListAsync());
    }

    [Fact]
    public async Task CycleNumbersAreIndependentPerUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        foreach (var userId in new[] { "user-a", "user-b" })
        {
            context.Lifts.AddRange(
                new Lift { UserId = userId, LiftType = LiftType.Squat, Name = $"{userId} Squat", TrainingMax = 300 },
                new Lift { UserId = userId, LiftType = LiftType.BenchPress, Name = $"{userId} Bench", TrainingMax = 200 },
                new Lift { UserId = userId, LiftType = LiftType.Deadlift, Name = $"{userId} Deadlift", TrainingMax = 350 },
                new Lift { UserId = userId, LiftType = LiftType.OverheadPress, Name = $"{userId} Press", TrainingMax = 135 });
        }
        await context.SaveChangesAsync();

        var cycleServiceA = new CycleService(
            context, new LiftService(context, new TestCurrentUserService("user-a")),
            new BbbMappingService(), new WeightCalculator(), new TestCurrentUserService("user-a"));
        var cycleServiceB = new CycleService(
            context, new LiftService(context, new TestCurrentUserService("user-b")),
            new BbbMappingService(), new WeightCalculator(), new TestCurrentUserService("user-b"));

        var cycleA = await cycleServiceA.CreateCycleAsync();
        var cycleB = await cycleServiceB.CreateCycleAsync();
        var nextA = await cycleServiceA.CreateCycleAsync();

        Assert.Equal(1, cycleA.CycleNumber);
        Assert.Equal(1, cycleB.CycleNumber);
        Assert.Equal(2, nextA.CycleNumber);
    }

    private sealed class TestCurrentUserService(string userId) : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult(userId);
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>(userId);
    }
}
