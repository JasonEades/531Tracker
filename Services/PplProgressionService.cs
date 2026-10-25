using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IPplProgressionService
{
    /// <summary>Returns true when every set is completed at or above repsMax — time to add weight.</summary>
    bool ShouldProgress(IEnumerable<PplSessionSet> sets, int repsMax);

    /// <summary>
    /// Increments the exercise's slot by ProgressionIncrement once. No-op when the slot has already
    /// moved past the weight this session was planned at, or when the exercise has not earned progression.
    /// </summary>
    Task<bool> ApplyProgressionAsync(int sessionExerciseId);

    /// <summary>Applies earned progression for every exercise in a completed session; returns the count applied.</summary>
    Task<int> ApplySessionProgressionAsync(int sessionId);

    /// <summary>Epley e1RM from the top-weight completed set of a session exercise.</summary>
    double? CalculateE1Rm(PplSessionExercise exercise);
}

public class PplProgressionService(AppDbContext db, IWeightCalculator weightCalc, ICurrentUserService userContext)
    : IPplProgressionService
{
    public bool ShouldProgress(IEnumerable<PplSessionSet> sets, int repsMax)
    {
        var all = sets.ToList();
        return all.Count > 0 && all.All(s => s.IsCompleted && s.ActualReps is int reps && reps >= repsMax);
    }

    public async Task<bool> ApplyProgressionAsync(int sessionExerciseId)
    {
        var userId = await userContext.GetUserIdAsync();
        var exercise = await db.PplSessionExercises
            .Include(e => e.Sets)
            .Include(e => e.ExerciseSlot)
            .FirstOrDefaultAsync(e => e.Id == sessionExerciseId && e.Session.Program.UserId == userId);
        if (exercise is null || !TryProgress(exercise)) return false;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> ApplySessionProgressionAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var exercises = await db.PplSessionExercises
            .Include(e => e.Sets)
            .Include(e => e.ExerciseSlot)
            .Where(e => e.PplSessionId == sessionId
                     && e.Session.Program.UserId == userId
                     && e.Session.Status == WorkoutStatus.Completed)
            .ToListAsync();

        var applied = exercises.Count(TryProgress);
        if (applied > 0) await db.SaveChangesAsync();
        return applied;
    }

    private bool TryProgress(PplSessionExercise exercise)
    {
        var slot = exercise.ExerciseSlot;
        if (slot.IsBodyweight || slot.ProgressionIncrement <= 0) return false;
        if (slot.CurrentWeight is not double current || exercise.SuggestedWeight is not double planned) return false;
        // Only progress from the weight this session was planned at, so repeated clicks or re-completing don't stack.
        if (current != planned) return false;
        // A one-off swap ("this workout only") must not progress the plan's original exercise.
        if (!string.Equals(exercise.ExerciseName, slot.ExerciseName, StringComparison.OrdinalIgnoreCase)) return false;
        if (!ShouldProgress(exercise.Sets, exercise.RepsMax)) return false;

        slot.CurrentWeight = current + slot.ProgressionIncrement;
        return true;
    }

    public double? CalculateE1Rm(PplSessionExercise exercise)
    {
        var topSet = exercise.Sets
            .Where(s => s.IsCompleted && s.ActualWeight.HasValue && s.ActualReps.HasValue && s.ActualReps > 0)
            .OrderByDescending(s => s.ActualWeight)
            .ThenByDescending(s => s.ActualReps)
            .FirstOrDefault();

        if (topSet is null) return null;
        return weightCalc.CalculateEstimated1RM(topSet.ActualWeight!.Value, topSet.ActualReps!.Value);
    }
}
