using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public sealed record GoogleHealthWarning(string Message);

public interface IGoogleHealthConnectionStatusService
{
    Task<GoogleHealthWarning?> GetWarningAsync(CancellationToken cancellationToken = default);
    Task MarkWarningShownAsync(CancellationToken cancellationToken = default);
}

public sealed class GoogleHealthConnectionStatusService(
    AppDbContext db,
    ICurrentUserService userContext) : IGoogleHealthConnectionStatusService
{
    private static readonly TimeSpan StaleSyncThreshold = TimeSpan.FromDays(14);
    private static readonly TimeSpan WarningInterval = TimeSpan.FromDays(7);

    public async Task<GoogleHealthWarning?> GetWarningAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        if (connection is null || connection.Status == HealthConnectionStatus.Revoked)
            return null;

        var now = DateTime.UtcNow;
        var requiresReconnect = connection.Status == HealthConnectionStatus.RequiresReconnect;
        var lastActivity = connection.LastSyncedAtUtc ?? connection.ConnectedAtUtc;
        var stale = connection.Status == HealthConnectionStatus.Connected &&
                     lastActivity <= now.Subtract(StaleSyncThreshold);
        var warningDue = connection.LastReconnectWarningAtUtc is null ||
                         connection.LastReconnectWarningAtUtc <= now.Subtract(WarningInterval);

        if ((!requiresReconnect && !stale) || !warningDue)
            return null;

        var message = requiresReconnect
            ? "Google Health is no longer authorized and daily steps are not syncing."
            : "Google Health has not synced successfully in the last 14 days.";
        return new GoogleHealthWarning(message);
    }

    public async Task MarkWarningShownAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken, tracked: true);
        if (connection is null || connection.Status == HealthConnectionStatus.Revoked)
            return;

        connection.LastReconnectWarningAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<GoogleHealthConnection?> GetConnectionAsync(
        CancellationToken cancellationToken,
        bool tracked = false)
    {
        var userId = await userContext.GetUserIdAsync();
        var query = db.GoogleHealthConnections.AsQueryable();
        if (!tracked)
            query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }
}
