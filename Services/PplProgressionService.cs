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

    /// <summary>Epley e1RM from the top-weight completed set of a session exercise.</summary>
    double? CalculateE1Rm(PplSessionExercise exercise);

    /// <summary>How many sessions have been completed for this slot since the last weight increase.</summary>
    Task<int> SessionsSinceLastProgressionAsync(int exerciseSlotId);
}

public class PplProgressionService(AppDbContext db, IWeightCalculator weightCalc, ICurrentUserService userContext)
    : IPplProgressionService
{
    public bool ShouldProgress(IEnumerable<PplSessionSet> sets, int repsMax)
    {
        var completed = sets.Where(s => s.IsCompleted && s.ActualReps.HasValue).ToList();
        return completed.Count > 0 && completed.All(s => s.ActualReps!.Value >= repsMax);
    }

    public async Task ApplyProgressionAsync(int exerciseSlotId)
    {
        var userId = await userContext.GetUserIdAsync();
        var slot = await db.PplExerciseSlots
            .Include(s => s.DayTemplate)
                .ThenInclude(d => d.Program)
            .FirstOrDefaultAsync(s => s.Id == exerciseSlotId && s.DayTemplate.Program.UserId == userId);
        if (slot is null || slot.IsBodyweight) return;

        if (!slot.CurrentWeight.HasValue) return;

        slot.CurrentWeight = weightCalc.RoundToNearest5(slot.CurrentWeight.Value + slot.ProgressionIncrement);
        await db.SaveChangesAsync();
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
        var userId = await userContext.GetUserIdAsync();
        var slot = await db.PplExerciseSlots
            .Include(s => s.DayTemplate)
                .ThenInclude(d => d.Program)
            .FirstOrDefaultAsync(s => s.Id == exerciseSlotId && s.DayTemplate.Program.UserId == userId);
        if (slot is null) return 0;

        // Count sessions where the slot was used at the current weight
        return await db.PplSessionExercises
            .Where(e => e.PplExerciseSlotId == exerciseSlotId
                     && e.Session.Program.UserId == userId
                     && e.SuggestedWeight == slot.CurrentWeight)
            .CountAsync();
    }
}
