using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public enum PplSwapScope
{
    /// <summary>Only this session uses the replacement; the plan and its progression are unchanged.</summary>
    ThisWorkout,

    /// <summary>The plan's exercise slot is replaced, so every future session uses the new exercise.</summary>
    FutureWorkouts
}

public interface IPplExerciseSwapService
{
    /// <summary>Library exercises (catalog + the user's custom) that train the same muscle group, closest movement first.</summary>
    Task<List<ExerciseDefinition>> GetSwapOptionsAsync(int sessionExerciseId);

    Task SwapAsync(int sessionExerciseId, int exerciseDefinitionId, PplSwapScope scope);
}

public class PplExerciseSwapService(
    AppDbContext db,
    ICurrentUserService userContext,
    IEstimatedStrengthService estimatedStrength,
    IWeightCalculator weightCalculator) : IPplExerciseSwapService
{
    public async Task<List<ExerciseDefinition>> GetSwapOptionsAsync(int sessionExerciseId)
    {
        var userId = await userContext.GetUserIdAsync();
        var exercise = await db.PplSessionExercises
            .Include(e => e.ExerciseSlot).ThenInclude(s => s.ExerciseDefinition)
            .FirstOrDefaultAsync(e => e.Id == sessionExerciseId && e.Session.Program.UserId == userId);
        if (exercise is null) return [];

        var muscle = exercise.ExerciseSlot.MuscleGroup;
        var movement = exercise.ExerciseSlot.ExerciseDefinition?.MovementType;
        var options = await db.ExerciseDefinitions.AsNoTracking()
            .Where(d => d.PrimaryMuscle == muscle && (d.UserId == null || d.UserId == userId))
            .ToListAsync();

        return options
            .Where(d => !string.Equals(d.Name, exercise.ExerciseName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => movement is not null && string.Equals(d.MovementType, movement, StringComparison.OrdinalIgnoreCase))
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SwapAsync(int sessionExerciseId, int exerciseDefinitionId, PplSwapScope scope)
    {
        var userId = await userContext.GetUserIdAsync();
        var exercise = await db.PplSessionExercises
            .Include(e => e.Session)
            .Include(e => e.Sets)
            .Include(e => e.ExerciseSlot)
            .FirstOrDefaultAsync(e => e.Id == sessionExerciseId && e.Session.Program.UserId == userId)
            ?? throw new InvalidOperationException("Exercise was not found.");
        if (exercise.Session.Status == WorkoutStatus.Completed)
            throw new InvalidOperationException("Reopen the session before swapping exercises.");
        if (exercise.Sets.Any(s => s.IsCompleted))
            throw new InvalidOperationException("Exercises with completed sets can't be swapped.");

        var definition = await db.ExerciseDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == exerciseDefinitionId && (d.UserId == null || d.UserId == userId))
            ?? throw new InvalidOperationException("The selected exercise is unavailable.");

        var isBodyweight = string.Equals(definition.Equipment.Trim(), "Bodyweight", StringComparison.OrdinalIgnoreCase);
        var profile = await estimatedStrength.GetProfileAsync(userId);
        var oneRepMax = isBodyweight ? null : profile.GetOneRepMax(definition.Name);
        double? seededWeight = oneRepMax is > 0
            ? weightCalculator.RoundToNearest5(oneRepMax.Value / (1 + exercise.RepsMax / 30.0))
            : null;

        if (scope == PplSwapScope.FutureWorkouts)
        {
            var slot = exercise.ExerciseSlot;
            slot.ExerciseName = definition.Name;
            slot.ExerciseDefinitionId = definition.Id;
            slot.MuscleGroup = definition.PrimaryMuscle;
            slot.IsBodyweight = isBodyweight;
            slot.StartingWeight = seededWeight;
            slot.CurrentWeight = seededWeight;
            if (isBodyweight) slot.ProgressionIncrement = 0;
            else if (slot.ProgressionIncrement <= 0) slot.ProgressionIncrement = 2.5;
        }

        exercise.ExerciseName = definition.Name;
        exercise.SuggestedWeight = seededWeight;
        foreach (var set in exercise.Sets)
        {
            set.ActualWeight = null;
            set.ActualReps = null;
        }

        await db.SaveChangesAsync();
    }
}
