using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public sealed class GoogleHealthSyncHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<GoogleHealthSyncHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private int running;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (Interlocked.Exchange(ref running, 1) == 1)
                continue;
            try
            {
                await SynchronizeUsersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Google Health scheduled synchronization failed.");
            }
            finally
            {
                Volatile.Write(ref running, 0);
            }
        }
    }

    private async Task SynchronizeUsersAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sync = scope.ServiceProvider.GetRequiredService<IGoogleHealthSyncService>();
        var end = DateTime.UtcNow.Date;
        var start = end.AddDays(-7);
        var userIds = await db.GoogleHealthConnections
            .AsNoTracking()
            .Where(x => x.Status == HealthConnectionStatus.Connected)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            try
            {
                await sync.SyncForUserAsync(userId, start, end, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Google Health synchronization failed for user {UserId}.", userId);
            }
        }
    }
}
