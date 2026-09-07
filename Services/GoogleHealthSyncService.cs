using System.Globalization;
using System.Text.Json;
using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public sealed record HealthSyncResult(int ImportedRecords, int CardioAdditions, int UpdatedRecords, int UnassignedRecords);

public interface IGoogleHealthSyncService
{
    Task<HealthSyncResult> SyncAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<HealthSyncResult> SyncForUserAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

public sealed class GoogleHealthSyncService(
    AppDbContext db,
    ICurrentUserService userContext,
    IGoogleHealthApiClient apiClient,
    ICycleDateResolver cycleDateResolver) : IGoogleHealthSyncService
{
    public async Task<HealthSyncResult> SyncAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var userId = await userContext.GetUserIdAsync();
        return await SyncForUserAsync(userId, startDate, endDate, cancellationToken);
    }

    public async Task<HealthSyncResult> SyncForUserAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var connection = await db.GoogleHealthConnections
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Status == HealthConnectionStatus.Connected, cancellationToken)
            ?? throw new InvalidOperationException("Google Health is not connected.");

        if (endDate.Date < startDate.Date)
            throw new ArgumentException("The sync end date must not precede the start date.");

        var payload = await apiClient.GetDailyStepsPayloadAsync(connection, startDate.Date, endDate.Date, cancellationToken);
        var incoming = ParseRecords(payload, startDate.Date, endDate.Date);
        var imported = 0;
        var updated = 0;
        var cardio = 0;
        var unassigned = 0;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var item in incoming)
        {
            var record = await db.DailyStepRecords
                .Include(x => x.CardioEntry)
                    .ThenInclude(x => x!.Session)
                .SingleOrDefaultAsync(x => x.UserId == userId && x.Provider == "GoogleHealth" &&
                    x.Metric == "DailySteps" && x.LocalDate == item.Date, cancellationToken);

            if (record is null)
            {
                record = new DailyStepRecord
                {
                    UserId = userId,
                    LocalDate = item.Date,
                    ProviderRecordId = item.ProviderRecordId,
                    StepCount = item.StepCount
                };
                db.DailyStepRecords.Add(record);
                imported++;
            }
            else
            {
                if (record.StepCount != item.StepCount)
                    updated++;
                record.StepCount = item.StepCount;
                record.ProviderRecordId = item.ProviderRecordId;
                record.UpdatedAtUtc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);
            var assignment = await cycleDateResolver.FindAssignmentAsync(item.Date);
            if (assignment is null)
            {
                unassigned++;
                continue;
            }

            var entry = record.CardioEntry;
            if (entry is null)
            {
                var accessory = await db.Accessories.SingleAsync(x => x.Category == AccessoryCategory.Cardio && x.IsActive && x.Name == "Steps", cancellationToken);
                var session = new AdditionalSession
                {
                    WeekId = assignment.Week.Id,
                    SessionType = SessionType.Cardio,
                    OccurredOn = item.Date,
                    CreatedAt = DateTime.UtcNow,
                    Name = "Google Health — Steps",
                    Notes = "Imported from Google Health"
                };
                db.AdditionalSessions.Add(session);
                await db.SaveChangesAsync(cancellationToken);
                entry = new CardioEntry
                {
                    AdditionalSessionId = session.Id,
                    AccessoryId = accessory.Id,
                    DailyStepRecordId = record.Id,
                    Quantity = item.StepCount,
                    Unit = CardioUnit.Steps,
                    Source = "Google Health",
                    Notes = "Imported from Google Health"
                };
                db.CardioEntries.Add(entry);
                cardio++;
            }
            else
            {
                entry.Quantity = item.StepCount;
                entry.Unit = CardioUnit.Steps;
                entry.Source = "Google Health";
                entry.Session.OccurredOn = item.Date;
                entry.Session.WeekId = assignment.Week.Id;
                entry.Session.CycleId = null;
                entry.Session.Name = "Google Health — Steps";
                entry.Session.Notes = "Imported from Google Health";
            }
        }

        connection.LastSyncedAtUtc = DateTime.UtcNow;
        connection.LastSyncError = null;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new HealthSyncResult(imported, cardio, updated, unassigned);
    }

    private static IReadOnlyList<IncomingStep> ParseRecords(string payload, DateTime startDate, DateTime endDate)
    {
        using var document = JsonDocument.Parse(payload);
        var values = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray()
            : document.RootElement.TryGetProperty("dataPoints", out var dataPoints)
                ? dataPoints.EnumerateArray()
                : document.RootElement.TryGetProperty("records", out var records)
                    ? records.EnumerateArray()
                : throw new InvalidOperationException("Google Health returned an unsupported daily steps response.");

        var result = new List<IncomingStep>();
        foreach (var value in values)
        {
            var dateText = GetDateText(value);
            if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                continue;
            var localDate = parsedDate.Date;
            if (localDate < startDate || localDate > endDate)
                continue;
            var count = GetStepCount(value);
            result.Add(new IncomingStep(localDate, Math.Max(0, count), value.TryGetProperty("id", out var id) ? id.GetString() : null));
        }
        return result.GroupBy(x => x.Date).Select(x => x.Last()).ToList();
    }

    private static string? GetDateText(JsonElement value)
    {
        foreach (var name in new[] { "date", "day", "startTime" })
        {
            if (value.TryGetProperty(name, out var property))
                return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
        }
        return null;
    }

    private static long GetStepCount(JsonElement value)
    {
        if (value.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Object &&
            steps.TryGetProperty("countSum", out var countSum))
            return ReadInt64(countSum);
        if (value.TryGetProperty("stepCount", out var stepCount))
            return ReadInt64(stepCount);
        return 0;
    }

    private static long ReadInt64(JsonElement value)
        => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)
            ? number
            : long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed : 0;

    private sealed record IncomingStep(DateTime Date, long StepCount, string? ProviderRecordId);
}
