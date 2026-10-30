using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IPplSessionService
{
    Task<PplSession?> GetSessionWithDetailsAsync(int sessionId);
    Task StartSessionAsync(int sessionId);
    Task CompleteSessionAsync(int sessionId);
    Task ReopenSessionAsync(int sessionId);
    Task UpdateSessionDateAsync(int sessionId, DateTime occurredOn);
    Task UpdateRestSecondsAsync(int sessionId, int restSeconds);
    Task UpdateSessionNotesAsync(int sessionId, string? notes);
    Task UpdateSetAsync(int setId, double? actualWeight, int? actualReps, bool isCompleted);
    Task<IReadOnlyList<int>> UpdateSetWeightAsync(int setId, double weight);
    Task SetStartingWeightAsync(int exerciseSlotId, double weight);
    /// <summary>Sets the bar used for plate loading (null = default bar) and remembers it for the plan exercise.</summary>
    Task SetExerciseBarAsync(int sessionExerciseId, int? barId);
    Task<List<PplSession>> GetSessionHistoryAsync(int programId, int take = 30);
    /// <summary>
    /// For each exercise in the session (keyed by session-exercise id), the completed sets (keyed by set number)
    /// from the most recent earlier PPL session of the user that included an exercise with the same name.
    /// </summary>
    Task<Dictionary<int, Dictionary<int, PplPreviousSet>>> GetPreviousSetsAsync(int sessionId);
}

public class PplSessionService(AppDbContext db, ICurrentUserService userContext) : IPplSessionService
{
    public const double MaxWeight = 2000;
    public const int MaxReps = 100;

    public static bool IsValidWeight(double weight) => double.IsFinite(weight) && weight is >= 0 and <= MaxWeight;
    public static bool IsValidReps(int reps) => reps is >= 0 and <= MaxReps;

    public async Task<PplSession?> GetSessionWithDetailsAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.PplSessions
            .Include(s => s.Program)
            .Include(s => s.DayTemplate)
            .Include(s => s.Week)
            .Include(s => s.Exercises.OrderBy(e => e.OrderInSession))
                .ThenInclude(e => e.ExerciseSlot)
                    .ThenInclude(s => s.Lift)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets.OrderBy(s => s.SetNumber))
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
    }

    public async Task StartSessionAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is not null && session.Status == WorkoutStatus.NotStarted)
        {
            session.Status = WorkoutStatus.InProgress;
            session.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateSessionDateAsync(int sessionId, DateTime occurredOn)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is null) return;

        session.OccurredOn = DateTime.SpecifyKind(occurredOn.Date, DateTimeKind.Utc);
        await db.SaveChangesAsync();
    }

    public async Task UpdateRestSecondsAsync(int sessionId, int restSeconds)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is null) return;

        session.RestSeconds = Math.Clamp(restSeconds, 0, 600);
        await db.SaveChangesAsync();
    }

    public async Task UpdateSessionNotesAsync(int sessionId, string? notes)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is null) return;

        session.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task SetStartingWeightAsync(int exerciseSlotId, double weight)
    {
        if (weight <= 0 || !IsValidWeight(weight)) return;

        var userId = await userContext.GetUserIdAsync();
        var slot = await db.PplExerciseSlots
            .Include(s => s.DayTemplate)
            .ThenInclude(d => d.Program)
            .FirstOrDefaultAsync(s => s.Id == exerciseSlotId && s.DayTemplate.Program.UserId == userId);
        if (slot is not null)
        {
            slot.StartingWeight ??= weight;
            slot.CurrentWeight ??= weight;
            await db.SaveChangesAsync();
        }
    }

    public async Task CompleteSessionAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is not null)
        {
            session.Status = WorkoutStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task ReopenSessionAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is not null && session.Status == WorkoutStatus.Completed)
        {
            session.Status = WorkoutStatus.InProgress;
            session.CompletedAt = null;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateSetAsync(int setId, double? actualWeight, int? actualReps, bool isCompleted)
    {
        if (actualWeight is double w && !IsValidWeight(w))
            throw new ArgumentOutOfRangeException(nameof(actualWeight), $"Weight must be between 0 and {MaxWeight}.");
        if (actualReps is int r && !IsValidReps(r))
            throw new ArgumentOutOfRangeException(nameof(actualReps), $"Reps must be between 0 and {MaxReps}.");

        var userId = await userContext.GetUserIdAsync();
        var set = await db.PplSessionSets
            .Include(s => s.SessionExercise)
                .ThenInclude(e => e.Session)
                    .ThenInclude(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == setId && s.SessionExercise.Session.Program.UserId == userId);
        if (set is not null)
        {
            set.ActualWeight = actualWeight;
            set.ActualReps = actualReps;
            set.IsCompleted = isCompleted;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Sets a set's weight and carries it forward to every later, not-yet-completed set of the same exercise.
    /// Returns the ids of all sets whose weight was changed.
    /// </summary>
    public async Task<Dictionary<int, Dictionary<int, PplPreviousSet>>> GetPreviousSetsAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var current = await db.PplSessions
            .Where(s => s.Id == sessionId && s.Program.UserId == userId)
            .Select(s => new
            {
                s.Id,
                s.OccurredOn,
                Exercises = s.Exercises.Select(e => new { e.Id, e.ExerciseName }).ToList()
            })
            .FirstOrDefaultAsync();
        if (current is null || current.Exercises.Count == 0)
            return [];

        var names = current.Exercises.Select(e => e.ExerciseName.ToLower()).Distinct().ToList();
        var rows = await db.PplSessionSets
            .Where(s => s.IsCompleted && s.ActualReps != null
                && s.SessionExercise.Session.Program.UserId == userId
                && s.SessionExercise.PplSessionId != current.Id
                && (s.SessionExercise.Session.OccurredOn < current.OccurredOn
                    || (s.SessionExercise.Session.OccurredOn == current.OccurredOn && s.SessionExercise.PplSessionId < current.Id))
                && names.Contains(s.SessionExercise.ExerciseName.ToLower()))
            .Select(s => new
            {
                Name = s.SessionExercise.ExerciseName,
                SessionId = s.SessionExercise.PplSessionId,
                s.SessionExercise.Session.OccurredOn,
                s.SetNumber,
                Reps = s.ActualReps!.Value,
                s.ActualWeight
            })
            .ToListAsync();

        var latestByName = rows
            .GroupBy(r => r.Name.ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(r => r.OccurredOn).ThenByDescending(r => r.SessionId).First();
                    return g.Where(r => r.SessionId == latest.SessionId)
                        .GroupBy(r => r.SetNumber)
                        .ToDictionary(sg => sg.Key, sg =>
                        {
                            var r = sg.First();
                            return new PplPreviousSet(r.SetNumber, r.Reps, r.ActualWeight, r.OccurredOn);
                        });
                });

        return current.Exercises
            .Where(e => latestByName.ContainsKey(e.ExerciseName.ToLowerInvariant()))
            .ToDictionary(e => e.Id, e => latestByName[e.ExerciseName.ToLowerInvariant()]);
    }

    public async Task<IReadOnlyList<int>> UpdateSetWeightAsync(int setId, double weight)
    {
        if (!IsValidWeight(weight))
            throw new ArgumentOutOfRangeException(nameof(weight), $"Weight must be between 0 and {MaxWeight}.");

        var userId = await userContext.GetUserIdAsync();
        var set = await db.PplSessionSets
            .Include(s => s.SessionExercise)
                .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.Id == setId && s.SessionExercise.Session.Program.UserId == userId);
        if (set is null)
            return [];

        var updated = new List<int>();
        foreach (var target in set.SessionExercise.Sets
                     .Where(s => s.Id == set.Id || (s.SetNumber > set.SetNumber && !s.IsCompleted)))
        {
            target.ActualWeight = weight;
            updated.Add(target.Id);
        }
        await db.SaveChangesAsync();
        return updated;
    }

    public async Task SetExerciseBarAsync(int sessionExerciseId, int? barId)
    {
        var userId = await userContext.GetUserIdAsync();
        var exercise = await db.PplSessionExercises
            .Include(e => e.ExerciseSlot)
            .FirstOrDefaultAsync(e => e.Id == sessionExerciseId && e.Session.Program.UserId == userId)
            ?? throw new InvalidOperationException("Exercise was not found.");
        if (barId is int id && !await db.Bars.AnyAsync(b => b.Id == id && b.UserId == userId))
            throw new InvalidOperationException("The selected bar is unavailable.");

        exercise.BarId = barId;
        // Remember the bar for future sessions of the plan exercise, but not for a one-off swap.
        if (string.Equals(exercise.ExerciseName, exercise.ExerciseSlot.ExerciseName, StringComparison.OrdinalIgnoreCase))
            exercise.ExerciseSlot.BarId = barId;
        await db.SaveChangesAsync();
    }

    public async Task<List<PplSession>> GetSessionHistoryAsync(int programId, int take = 30)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.PplSessions
            .Include(s => s.DayTemplate)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets)
            .Where(s => s.PplProgramId == programId && s.Program.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}
