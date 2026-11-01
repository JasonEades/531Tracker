using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public sealed record ManualOneRepMaxResult(bool Success, string? Error, ManualOneRepMax? Entry = null);

public interface IManualOneRepMaxService
{
    Task<List<ManualOneRepMax>> GetEntriesAsync();
    /// <summary>Adds a new entry (Id 0) or updates the user's existing entry with the same id.</summary>
    Task<ManualOneRepMaxResult> SaveAsync(int id, string exerciseName, double weight, DateTime achievedOn, string? notes);
    Task<bool> DeleteAsync(int id);
    /// <summary>Exercise names the user has logged plus the exercise library, so entries line up with existing history.</summary>
    Task<List<string>> GetExerciseNameSuggestionsAsync();
}

public sealed class ManualOneRepMaxService(
    AppDbContext db,
    ICurrentUserService userContext,
    IWeightCalculator weightCalculator) : IManualOneRepMaxService
{
    public const int MaxNameLength = 100;
    public const int MaxNotesLength = 500;

    public async Task<List<ManualOneRepMax>> GetEntriesAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.ManualOneRepMaxes.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AchievedOn)
            .ThenBy(x => x.ExerciseName)
            .ToListAsync();
    }

    public async Task<ManualOneRepMaxResult> SaveAsync(int id, string exerciseName, double weight, DateTime achievedOn, string? notes)
    {
        var name = exerciseName?.Trim() ?? string.Empty;
        if (name.Length == 0) return new(false, "Enter an exercise name.");
        if (name.Length > MaxNameLength) return new(false, $"Exercise name must be {MaxNameLength} characters or fewer.");
        if (!PplSessionService.IsValidWeight(weight) || weight <= 0)
            return new(false, $"1RM must be between 0 and {PplSessionService.MaxWeight} lb.");
        var date = achievedOn.Date;
        if (date > DateTime.UtcNow.Date.AddDays(1)) return new(false, "Date can't be in the future.");
        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmedNotes?.Length > MaxNotesLength) return new(false, $"Notes must be {MaxNotesLength} characters or fewer.");

        var userId = await userContext.GetUserIdAsync();
        ManualOneRepMax? entry;
        if (id == 0)
        {
            entry = new ManualOneRepMax { UserId = userId };
            db.ManualOneRepMaxes.Add(entry);
        }
        else
        {
            entry = await db.ManualOneRepMaxes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (entry is null) return new(false, "Entry not found.");
        }

        entry.ExerciseName = await CanonicalNameAsync(userId, name);
        entry.Weight = weight;
        entry.AchievedOn = date;
        entry.Notes = trimmedNotes;
        entry.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new(true, null, entry);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var userId = await userContext.GetUserIdAsync();
        var entry = await db.ManualOneRepMaxes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (entry is null) return false;

        db.ManualOneRepMaxes.Remove(entry);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<string>> GetExerciseNameSuggestionsAsync() =>
        await LoadKnownNamesAsync(await userContext.GetUserIdAsync());

    /// <summary>Reuses the spelling of an already-known exercise so the entry joins that exercise's history.</summary>
    private async Task<string> CanonicalNameAsync(string userId, string name)
    {
        var key = ExerciseHistoryService.BuildKey(name);
        var known = await LoadKnownNamesAsync(userId);
        return known.FirstOrDefault(n => ExerciseHistoryService.BuildKey(n) == key) ?? name;
    }

    private async Task<List<string>> LoadKnownNamesAsync(string userId)
    {
        // Logged names come first so a match prefers how the user actually recorded the exercise.
        var logged = (await ExerciseObservations.LoadAsync(db, userId, weightCalculator, includeManual: true))
            .Select(o => o.Name);
        var lifts = await db.Lifts.AsNoTracking().Where(l => l.UserId == userId).Select(l => l.Name).ToListAsync();
        var library = await db.ExerciseDefinitions.AsNoTracking()
            .Where(d => d.UserId == null || d.UserId == userId)
            .Select(d => d.Name)
            .ToListAsync();

        return logged.Concat(lifts).Concat(library)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .DistinctBy(ExerciseHistoryService.BuildKey)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
