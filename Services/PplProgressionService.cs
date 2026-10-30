using FiveThreeOneTracker.Models;

namespace FiveThreeOneTracker.Services;

/// <summary>
/// PPL weights are never increased automatically: the user adjusts weights manually in a session and
/// <see cref="PplSessionService"/> carries the weight they actually lifted forward to future sessions.
/// </summary>
public interface IPplProgressionService
{
    /// <summary>Epley e1RM from the top-weight completed set of a session exercise.</summary>
    double? CalculateE1Rm(PplSessionExercise exercise);
}

public class PplProgressionService(IWeightCalculator weightCalc) : IPplProgressionService
{
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
