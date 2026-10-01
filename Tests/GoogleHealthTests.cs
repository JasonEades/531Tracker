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

        var resolver = new CycleDateResolver(db);
        var assignment = await resolver.FindAssignmentAsync("test-user", new DateTime(2026, 9, 8));
        var outside = await resolver.FindAssignmentAsync("test-user", new DateTime(2026, 10, 1));

        Assert.NotNull(assignment);
        Assert.Equal(WeekNumber.Week2, assignment!.Week.WeekNumber);
        Assert.Null(outside);
    }

    [Fact]
    public async Task CycleDateResolverDoesNotAssignAnotherUsersCycle()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var cycle = new Cycle { UserId = "other-user", Name = "Other Cycle", StartDate = new DateTime(2026, 9, 1) };
        cycle.Weeks.Add(new Week { WeekNumber = WeekNumber.Week1 });
        db.Cycles.Add(cycle);
        await db.SaveChangesAsync();

        var resolver = new CycleDateResolver(db);

        Assert.Null(await resolver.FindAssignmentAsync("test-user", new DateTime(2026, 9, 2)));
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

    [Fact]
    public async Task GoogleHealthWarningDetectsStaleSyncAndThrottlesForSevenDays()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Users.Add(new ApplicationUser { Id = "test-user", UserName = "test-user", NormalizedUserName = "TEST-USER" });
        db.GoogleHealthConnections.Add(new GoogleHealthConnection
        {
            UserId = "test-user",
            GoogleSubject = "subject",
            EncryptedAccessToken = "token",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            GrantedScopes = "scope",
            ConnectedAtUtc = DateTime.UtcNow.AddDays(-30),
            LastSyncedAtUtc = DateTime.UtcNow.AddDays(-15)
        });
        await db.SaveChangesAsync();

        var service = new GoogleHealthConnectionStatusService(db, new TestCurrentUserService());

        Assert.NotNull(await service.GetWarningAsync());
        await service.MarkWarningShownAsync();
        Assert.Null(await service.GetWarningAsync());

        var stored = await db.GoogleHealthConnections.SingleAsync();
        stored.LastReconnectWarningAtUtc = DateTime.UtcNow.AddDays(-8);
        await db.SaveChangesAsync();

        Assert.NotNull(await service.GetWarningAsync());
    }

    [Fact]
    public async Task GoogleHealthWarningDetectsReconnectRequiredButNotRevoked()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Users.Add(new ApplicationUser { Id = "test-user", UserName = "test-user", NormalizedUserName = "TEST-USER" });
        db.GoogleHealthConnections.Add(new GoogleHealthConnection
        {
            UserId = "test-user",
            GoogleSubject = "subject",
            EncryptedAccessToken = "token",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            GrantedScopes = "scope",
            Status = HealthConnectionStatus.RequiresReconnect,
            LastSyncedAtUtc = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        var service = new GoogleHealthConnectionStatusService(db, new TestCurrentUserService());
        Assert.Contains("no longer authorized", (await service.GetWarningAsync())!.Message);

        var stored = await db.GoogleHealthConnections.SingleAsync();
        stored.Status = HealthConnectionStatus.Revoked;
        await db.SaveChangesAsync();

        Assert.Null(await service.GetWarningAsync());
    }

    [Fact]
    public async Task GoogleHealthWarningDoesNothingForAnonymousUsers()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var service = new GoogleHealthConnectionStatusService(db, new AnonymousCurrentUserService());

        Assert.Null(await service.GetWarningAsync());
        await service.MarkWarningShownAsync();
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() => Task.FromResult("test-user");
        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>("test-user");
    }

    private sealed class AnonymousCurrentUserService : ICurrentUserService
    {
        public Task<string> GetUserIdAsync() =>
            throw new InvalidOperationException("User is not authenticated.");

        public Task<string?> GetUserIdOrNullAsync() => Task.FromResult<string?>(null);
    }
}
