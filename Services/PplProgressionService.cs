using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IPplProgressionService
{
    /// <summary>Returns true if all completed sets hit or exceeded repsMax — time to add weight.</summary>
    bool ShouldProgress(IEnumerable<PplSessionSet> sets, int repsMax);

    /// <summary>Increments CurrentWeight on the slot by ProgressionIncrement and persists.</summary>
    Task ApplyProgressionAsync(int exerciseSlotId);

    /// <summary>
    /// Writes the best estimated 1RM from recent sessions back to Lift.TrainingMax.
    /// Only applies for slots linked to a Lift.
    /// </summary>
    Task SyncToTrainingMaxAsync(int exerciseSlotId);

    /// <summary>Epley e1RM from the top-weight completed set of a session exercise.</summary>
    double? CalculateE1Rm(PplSessionExercise exercise);

    /// <summary>How many sessions have been completed for this slot since the last weight increase.</summary>
    Task<int> SessionsSinceLastProgressionAsync(int exerciseSlotId);
}

public class PplProgressionService(AppDbContext db, IWeightCalculator weightCalc, ILiftService liftService)
    : IPplProgressionService
{
    public bool ShouldProgress(IEnumerable<PplSessionSet> sets, int repsMax)
    {
        var completed = sets.Where(s => s.IsCompleted && s.ActualReps.HasValue).ToList();
        return completed.Count > 0 && completed.All(s => s.ActualReps!.Value >= repsMax);
    }

    public async Task ApplyProgressionAsync(int exerciseSlotId)
    {
        var slot = await db.PplExerciseSlots.FindAsync(exerciseSlotId);
        if (slot is null || slot.IsBodyweight) return;

        slot.CurrentWeight = weightCalc.RoundToNearest5(slot.CurrentWeight + slot.ProgressionIncrement);
        await db.SaveChangesAsync();
    }

    public async Task SyncToTrainingMaxAsync(int exerciseSlotId)
    {
        var slot = await db.PplExerciseSlots
            .Include(s => s.Lift)
            .FirstOrDefaultAsync(s => s.Id == exerciseSlotId);

        if (slot?.LiftId is null) return;

        // Find the highest e1RM across all session sets for this slot
        var allSets = await db.PplSessionSets
            .Where(s => s.SessionExercise.PplExerciseSlotId == exerciseSlotId
                     && s.IsCompleted
                     && s.ActualWeight.HasValue
                     && s.ActualReps.HasValue
                     && s.ActualReps > 0)
            .ToListAsync();

        if (allSets.Count == 0) return;

        var bestE1Rm = allSets
            .Select(s => weightCalc.CalculateEstimated1RM(s.ActualWeight!.Value, s.ActualReps!.Value))
            .Max();

        var newTm = weightCalc.RoundToNearest5(bestE1Rm * 0.9);

        await liftService.UpdateTrainingMaxAsync(slot.LiftId.Value, newTm);
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

    public async Task<int> SessionsSinceLastProgressionAsync(int exerciseSlotId)
    {
        var slot = await db.PplExerciseSlots.FindAsync(exerciseSlotId);
        if (slot is null) return 0;

        // Count sessions where the slot was used at the current weight
        return await db.PplSessionExercises
            .Where(e => e.PplExerciseSlotId == exerciseSlotId
                     && e.SuggestedWeight == slot.CurrentWeight)
            .CountAsync();
    }
}
