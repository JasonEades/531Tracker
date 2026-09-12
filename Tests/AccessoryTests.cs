using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class AccessoryTests
{
    [Fact]
    public async Task SuggestedUsageUsesLatestWorkoutValuesIncludingNotes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cycle = new Cycle { UserId = "test-user", Name = "Cycle" };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var accessory = new Accessory { Name = "Rows" };
        var earlierWorkout = new Workout
        {
            Week = week,
            MainLiftType = LiftType.BenchPress,
            OccurredOn = new DateTime(2026, 9, 1)
        };
        var latestWorkout = new Workout
        {
            Week = week,
            MainLiftType = LiftType.OverheadPress,
            OccurredOn = new DateTime(2026, 9, 8)
        };
        earlierWorkout.WorkoutAccessories.Add(new WorkoutAccessory
        {
            Accessory = accessory,
            Weight = 100,
            Reps = 10,
            Sets = 3,
            Notes = "Earlier notes"
        });
        latestWorkout.WorkoutAccessories.Add(new WorkoutAccessory
        {
            Accessory = accessory,
            Weight = 110,
            Reps = 8,
            Sets = 4,
            Notes = "Latest notes"
        });
        context.Cycles.Add(cycle);
        context.Workouts.AddRange(earlierWorkout, latestWorkout);
        await context.SaveChangesAsync();
        Assert.Equal(2, await context.WorkoutAccessories.CountAsync());
        Assert.Equal("test-user", await context.Workouts
            .Select(w => w.Week.Cycle.UserId)
            .Distinct()
            .SingleAsync());

        var service = new AccessoryService(context, new TestCurrentUserService());
        var suggestion = await service.GetSuggestedUsageAsync(accessory.Id);

        Assert.NotNull(suggestion);
        Assert.Equal(110, suggestion!.Weight);
        Assert.Equal(8, suggestion.Reps);
        Assert.Equal(4, suggestion.Sets);
        Assert.Equal("Latest notes", suggestion.Notes);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult("test-user");
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>("test-user");
    }
}
