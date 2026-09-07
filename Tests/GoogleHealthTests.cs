using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class GoogleHealthTests
{
    [Fact]
    public async Task CycleDateResolverUsesHealthDateAndLeavesOutsideDatesUnassigned()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var cycle = new Cycle { UserId = "test-user", Name = "Cycle 12", StartDate = new DateTime(2026, 9, 1) };
        for (var i = 1; i <= 4; i++)
            cycle.Weeks.Add(new Week { WeekNumber = (WeekNumber)i });
        db.Cycles.Add(cycle);
        await db.SaveChangesAsync();

        var resolver = new CycleDateResolver(db, new TestCurrentUserService());
        var assignment = await resolver.FindAssignmentAsync(new DateTime(2026, 9, 8));
        var outside = await resolver.FindAssignmentAsync(new DateTime(2026, 10, 1));

        Assert.NotNull(assignment);
        Assert.Equal(WeekNumber.Week2, assignment!.Week.WeekNumber);
        Assert.Null(outside);
    }

    [Fact]
    public async Task DailyStepRecordsAreUniquePerUserProviderMetricAndDate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.DailyStepRecords.AddRange(
            new DailyStepRecord { UserId = "test-user", LocalDate = new DateTime(2026, 9, 7), StepCount = 5421 },
            new DailyStepRecord { UserId = "test-user", LocalDate = new DateTime(2026, 9, 7), StepCount = 8421 });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public void ImportedStepsRemainSupplementalActivity()
    {
        var analytics = new CurrentProgramAnalytics
        {
            PlannedSessions = 4,
            CompletedSessions = 4,
            AdditionalSessions = 7
        };

        Assert.Equal(100, analytics.CompletionPercent);
        Assert.Equal(7, analytics.AdditionalSessions);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult("test-user");
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>("test-user");
    }
}
