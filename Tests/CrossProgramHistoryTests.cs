using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class CrossProgramHistoryTests
{
    [Fact]
    public async Task PauseKeepsSessionsAndEndRemovesOnlyUntouchedSessions()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateContextAsync(connection);
        var (program, logged, untouched) = await SeedPplProgramAsync(context, "owner");

        var service = CreateProgramService(context, "owner");
        await service.PauseProgramAsync(program.Id);
        context.ChangeTracker.Clear();
        var paused = await context.PplPrograms.SingleAsync(p => p.Id == program.Id);
        Assert.Equal("Paused", paused.StatusLabel);
        Assert.Equal(2, await context.PplSessions.CountAsync());

        await service.EndProgramAsync(program.Id);
        context.ChangeTracker.Clear();
        var ended = await context.PplPrograms.SingleAsync(p => p.Id == program.Id);
        Assert.False(ended.IsActive);
        Assert.NotNull(ended.EndedAt);
        Assert.Equal("Ended", ended.StatusLabel);
        Assert.Equal([logged.Id], await context.PplSessions.Select(s => s.Id).ToListAsync());
        Assert.DoesNotContain(untouched.Id, await context.PplSessions.Select(s => s.Id).ToListAsync());

        await service.SetActiveProgramAsync(program.Id);
        context.ChangeTracker.Clear();
        var resumed = await context.PplPrograms.SingleAsync(p => p.Id == program.Id);
        Assert.True(resumed.IsActive);
        Assert.Null(resumed.EndedAt);
    }

    [Fact]
    public async Task OtherUsersCannotPauseOrEndProgram()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateContextAsync(connection);
        var (program, _, _) = await SeedPplProgramAsync(context, "owner");

        var intruder = CreateProgramService(context, "intruder");
        await intruder.PauseProgramAsync(program.Id);
        await intruder.EndProgramAsync(program.Id);

        context.ChangeTracker.Clear();
        var stored = await context.PplPrograms.SingleAsync(p => p.Id == program.Id);
        Assert.True(stored.IsActive);
        Assert.Null(stored.EndedAt);
        Assert.Equal(2, await context.PplSessions.CountAsync());
    }

    [Fact]
    public async Task PplPreviousSetsFallBackToFiveThreeOneAccessoryWithCloseName()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateContextAsync(connection);
        await SeedAccessoryLogAsync(context, "owner", "Face Pull", 40, 15, new DateTime(2026, 9, 1));
        await SeedAccessoryLogAsync(context, "intruder", "Face Pulls", 90, 5, new DateTime(2026, 9, 5));
        var (_, _, untouched) = await SeedPplProgramAsync(context, "owner", exerciseName: "Face Pulls", loggedName: "Bench Press");

        var previous = await new PplSessionService(context, new TestCurrentUserService("owner"))
            .GetPreviousSetsAsync(untouched.Id);

        var exerciseId = untouched.Exercises.Single().Id;
        var sets = previous[exerciseId];
        Assert.Equal(3, sets.Count);
        Assert.All(sets.Values, set =>
        {
            Assert.Equal(40, set.Weight);
            Assert.Equal(15, set.Reps);
            Assert.Equal("5/3/1", set.Source);
        });
    }

    [Fact]
    public async Task LibrarySuggestionUsesPplHistoryForOwnerOnly()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateContextAsync(connection);
        var definition = new ExerciseDefinition { Name = "Chest Supported Row", UserId = "owner" };
        context.ExerciseDefinitions.Add(definition);
        await context.SaveChangesAsync();
        await SeedPplProgramAsync(context, "owner", loggedName: "Chest Supported Row", loggedWeight: 70);

        var suggestion = await new AccessoryService(context, new TestCurrentUserService("owner"))
            .GetSuggestedUsageAsync($"d:{definition.Id}");
        Assert.NotNull(suggestion);
        Assert.Equal(70, suggestion.Weight);
        Assert.Equal(8, suggestion.Reps);
        Assert.Equal(3, suggestion.Sets);
        Assert.Equal("PPL", suggestion.Source);
        Assert.NotNull(suggestion.LastPerformed);

        Assert.Null(await new AccessoryService(context, new TestCurrentUserService("intruder"))
            .GetSuggestedUsageAsync($"d:{definition.Id}"));
    }

    [Fact]
    public async Task StartingEstimatesIncludeAccessoryLogsWithFuzzyNames()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateContextAsync(connection);
        await SeedAccessoryLogAsync(context, "owner", "Face Pull", 50, 10, new DateTime(2026, 9, 1));

        var estimates = new EstimatedStrengthService(context, new WeightCalculator());
        var oneRepMax = (await estimates.GetProfileAsync("owner")).GetOneRepMax("Face Pulls");
        Assert.NotNull(oneRepMax);
        Assert.True(oneRepMax > 50);

        Assert.Null((await estimates.GetProfileAsync("intruder")).GetOneRepMax("Face Pulls"));
    }

    private static PplProgramService CreateProgramService(AppDbContext context, string userId)
    {
        var calculator = new WeightCalculator();
        return new PplProgramService(
            context, new TestCurrentUserService(userId), new EstimatedStrengthService(context, calculator), calculator);
    }

    private static async Task<SqliteConnection> OpenAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<AppDbContext> CreateContextAsync(SqliteConnection connection)
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        context.Users.AddRange(
            new ApplicationUser { Id = "owner", UserName = "owner" },
            new ApplicationUser { Id = "intruder", UserName = "intruder" });
        await context.SaveChangesAsync();
        return context;
    }

    private static async Task SeedAccessoryLogAsync(
        AppDbContext context, string userId, string accessoryName, double weight, int reps, DateTime date)
    {
        var cycle = new Cycle { UserId = userId, Name = $"{userId} cycle" };
        var week = new Week { Cycle = cycle, WeekNumber = WeekNumber.Week1 };
        var workout = new Workout { Week = week, MainLiftType = LiftType.BenchPress, OccurredOn = date, Status = WorkoutStatus.Completed };
        workout.WorkoutAccessories.Add(new WorkoutAccessory
        {
            Accessory = new Accessory { Name = accessoryName, UserId = userId },
            Weight = weight,
            Reps = reps,
            Sets = 3,
            IsCompleted = true
        });
        context.Cycles.Add(cycle);
        context.Workouts.Add(workout);
        await context.SaveChangesAsync();
    }

    private static async Task<(PplProgram Program, PplSession Logged, PplSession Untouched)> SeedPplProgramAsync(
        AppDbContext context,
        string userId,
        string exerciseName = "Lat Pulldown",
        string loggedName = "Lat Pulldown",
        double loggedWeight = 100)
    {
        var program = new PplProgram { UserId = userId, Name = "PPL", DaysPerWeek = 3, IsActive = true };
        var day = new PplDayTemplate { Program = program, DayType = PplDayType.Pull, OrderInWeek = 1, Name = "Pull" };
        var loggedSlot = new PplExerciseSlot { OrderInDay = 1, ExerciseName = loggedName };
        var nextSlot = new PplExerciseSlot { OrderInDay = 2, ExerciseName = exerciseName };
        day.ExerciseSlots.Add(loggedSlot);
        day.ExerciseSlots.Add(nextSlot);

        var logged = new PplSession
        {
            Program = program, DayTemplate = day, Status = WorkoutStatus.Completed, OccurredOn = new DateTime(2026, 9, 10)
        };
        var loggedExercise = new PplSessionExercise
        {
            ExerciseSlot = loggedSlot, ExerciseName = loggedName, TargetSets = 3, RepsMin = 8, RepsMax = 12, OrderInSession = 1
        };
        for (var i = 1; i <= 3; i++)
            loggedExercise.Sets.Add(new PplSessionSet
            {
                SetNumber = i, TargetReps = 8, ActualReps = 8, ActualWeight = loggedWeight, IsCompleted = true
            });
        logged.Exercises.Add(loggedExercise);

        var untouched = new PplSession { Program = program, DayTemplate = day, OccurredOn = new DateTime(2026, 9, 20) };
        var untouchedExercise = new PplSessionExercise
        {
            ExerciseSlot = nextSlot, ExerciseName = exerciseName, TargetSets = 3, RepsMin = 8, RepsMax = 12, OrderInSession = 1
        };
        for (var i = 1; i <= 3; i++)
            untouchedExercise.Sets.Add(new PplSessionSet { SetNumber = i, TargetReps = 8 });
        untouched.Exercises.Add(untouchedExercise);

        context.PplPrograms.Add(program);
        context.PplSessions.AddRange(logged, untouched);
        await context.SaveChangesAsync();
        return (program, logged, untouched);
    }

    private sealed class TestCurrentUserService(string userId) : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult(userId);
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>(userId);
    }
}
