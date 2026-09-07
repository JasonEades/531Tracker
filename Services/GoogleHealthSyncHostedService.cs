using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FiveThreeOneTracker.Services;

public sealed class GoogleHealthSyncHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<GoogleHealthOptions> options,
    ILogger<GoogleHealthSyncHostedService> logger) : BackgroundService
{
    private int running;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun(DateTime.UtcNow, options.Value.SyncHourUtc);
            logger.LogInformation("Next Google Health synchronization scheduled in {Delay}.", delay);
            await Task.Delay(delay, stoppingToken);

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
        List<string> userIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            userIds = await db.GoogleHealthConnections
                .AsNoTracking()
                .Where(x => x.Status == HealthConnectionStatus.Connected)
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);
        }

        var end = DateTime.UtcNow.Date;
        var start = end.AddDays(-7);

        foreach (var userId in userIds)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sync = scope.ServiceProvider.GetRequiredService<IGoogleHealthSyncService>();
                await sync.SyncForUserAsync(userId, start, end, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Google Health synchronization failed for user {UserId}.", userId);
            }
        }
    }

    private static TimeSpan DelayUntilNextRun(DateTime nowUtc, int syncHourUtc)
    {
        var hour = Math.Clamp(syncHourUtc, 0, 23);
        var next = nowUtc.Date.AddHours(hour);
        if (next <= nowUtc)
            next = next.AddDays(1);
        return next - nowUtc;
    }
}
