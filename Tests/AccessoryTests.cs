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

    [Fact]
    public async Task SettingAnotherUsersBarAsDefaultDoesNotClearCurrentUsersDefault()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var ownBar = new Bar { UserId = "test-user", Name = "My Bar", Weight = 45, IsDefault = true };
        var otherBar = new Bar { UserId = "other-user", Name = "Other Bar", Weight = 35, IsDefault = true };
        context.Bars.AddRange(ownBar, otherBar);
        await context.SaveChangesAsync();

        var service = new PlateCalculatorService(context, new TestCurrentUserService());
        await service.SetDefaultBarAsync(otherBar.Id);

        Assert.True((await context.Bars.SingleAsync(bar => bar.Id == ownBar.Id)).IsDefault);
        Assert.True((await context.Bars.SingleAsync(bar => bar.Id == otherBar.Id)).IsDefault);
    }

    [Fact]
    public async Task AccessoryHistoryCannotBeRecordedForAnotherUsersPrivateAccessory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var accessory = new Accessory { UserId = "other-user", Name = "Private Accessory" };
        context.Accessories.Add(accessory);
        await context.SaveChangesAsync();

        var service = new AccessoryService(context, new TestCurrentUserService());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordAccessoryHistoryAsync(accessory.Id, 100, 10, 3));
        Assert.Empty(await context.AccessoryHistory.ToListAsync());
    }

    [Fact]
    public async Task LatestAccessoryWeightsAreUserScopedAndUseMostRecentEntry()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var facePull = new Accessory { Name = "Face Pulls" };
        var otherPrivate = new Accessory { UserId = "other-user", Name = "Other Private Curl" };
        var ownCycle = new Cycle { UserId = "test-user", Name = "Mine" };
        var otherCycle = new Cycle { UserId = "other-user", Name = "Theirs" };
        var ownWorkout = new Workout { Week = new Week { Cycle = ownCycle, WeekNumber = WeekNumber.Week1 }, MainLiftType = LiftType.Squat, OccurredOn = new DateTime(2026, 9, 1) };
        var otherWorkout = new Workout { Week = new Week { Cycle = otherCycle, WeekNumber = WeekNumber.Week1 }, MainLiftType = LiftType.Squat, OccurredOn = new DateTime(2026, 10, 1) };
        ownWorkout.WorkoutAccessories.Add(new WorkoutAccessory { Accessory = facePull, Weight = 40, Reps = 15, Sets = 3 });
        otherWorkout.WorkoutAccessories.Add(new WorkoutAccessory { Accessory = facePull, Weight = 200, Reps = 15, Sets = 3 });
        otherWorkout.WorkoutAccessories.Add(new WorkoutAccessory { Accessory = otherPrivate, Weight = 60, Reps = 10, Sets = 3 });
        context.Workouts.AddRange(ownWorkout, otherWorkout);
        context.AccessoryHistory.Add(new AccessoryHistory { Accessory = facePull, UserId = "test-user", Weight = 45, Reps = 15, Sets = 3, RecordedAt = new DateTime(2026, 9, 10) });
        context.AccessoryHistory.Add(new AccessoryHistory { Accessory = facePull, UserId = "other-user", Weight = 300, Reps = 15, Sets = 3, RecordedAt = new DateTime(2026, 10, 10) });
        await context.SaveChangesAsync();

        var mine = await new AccessoryService(context, new TestCurrentUserService()).GetLatestWeightsAsync();

        var option = Assert.Single(mine);
        Assert.Equal(facePull.Id, option.AccessoryId);
        Assert.Equal(45, option.Weight);
        Assert.Equal(15, option.Reps);
    }

    [Theory]
    [InlineData("Face Pulls", "Face Pull")]
    [InlineData("Incline DB Press", "Incline Dumbbell Press")]
    [InlineData("Tricep Dips", "Dips")]
    [InlineData("Overhead Tricep Ext", "Overhead Tricep Extension")]
    [InlineData("Lat Pulldown", "Lat Pull-Down")]
    [InlineData("Romanian Deadlift", "RDL")]
    [InlineData("Weighted Pull-ups", "Pull Ups")]
    [InlineData("Standing Calf Raise", "Calf Raises")]
    [InlineData("Tricep Pushdown", "Cable Tricep Pushdown")]
    [InlineData("Shrug", "Dumbbell Shrugs")]
    public void ExerciseNameMatcherAutoMatchesEquivalentNames(string plan, string accessory) =>
        Assert.True(ExerciseNameMatcher.Score(plan, accessory) >= ExerciseNameMatcher.AutoMatchThreshold,
            $"{plan} vs {accessory} scored {ExerciseNameMatcher.Score(plan, accessory)}");

    [Theory]
    [InlineData("Dumbbell Curl", "Leg Curl")]
    [InlineData("Barbell Row", "Dumbbell Row")]
    [InlineData("Cable Row", "Barbell Row")]
    [InlineData("Hammer Curl", "Dumbbell Curl")]
    [InlineData("Romanian Deadlift", "Deadlift")]
    [InlineData("Leg Press", "Leg Curl")]
    [InlineData("Overhead Press", "DB Shoulder Press")]
    [InlineData("Weighted Pull-ups", "Chin-Ups")]
    [InlineData("Lateral Raise", "Front Raise")]
    public void ExerciseNameMatcherRejectsDifferentMovements(string plan, string accessory) =>
        Assert.True(ExerciseNameMatcher.Score(plan, accessory) < ExerciseNameMatcher.AutoMatchThreshold,
            $"{plan} vs {accessory} scored {ExerciseNameMatcher.Score(plan, accessory)}");

    [Fact]
    public void AccessoryWeightConvertsToPplRepRange()
    {
        var inRange = new AccessoryWeightOption(1, "Cable Fly", 30, 12, DateTime.UtcNow);
        Assert.Equal(30, inRange.WeightForRepRange(12, 15));

        // 100 × 5 ≈ 116.7 e1RM → 15 reps ≈ 77.8 → 77.5
        var heavy = new AccessoryWeightOption(2, "Dumbbell Row", 100, 5, DateTime.UtcNow);
        Assert.Equal(77.5, heavy.WeightForRepRange(10, 15));
    }

    private sealed class TestCurrentUserService(string userId = "test-user") : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult(userId);
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>(userId);
    }
}
