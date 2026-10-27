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

    /// <summary>The user's generic (non-cardio) accessories, likely name matches first, then most recently used.</summary>
    Task<List<PplAccessorySwapOption>> GetAccessorySwapOptionsAsync(int sessionExerciseId);

    /// <summary>Swaps to a generic accessory, starting from the last weight logged for it.</summary>
    Task SwapToAccessoryAsync(int sessionExerciseId, int accessoryId, PplSwapScope scope);
}

public sealed record PplAccessorySwapOption(int AccessoryId, string Name, double? LastWeight, int? LastReps, DateTime? LastUsed, bool LikelyMatch);

public class PplExerciseSwapService(
    AppDbContext db,
    ICurrentUserService userContext,
    IEstimatedStrengthService estimatedStrength,
    IWeightCalculator weightCalculator,
    IAccessoryService accessoryService) : IPplExerciseSwapService
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
        var exercise = await LoadSwappableExerciseAsync(sessionExerciseId, userId);

        var definition = await db.ExerciseDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == exerciseDefinitionId && (d.UserId == null || d.UserId == userId))
            ?? throw new InvalidOperationException("The selected exercise is unavailable.");

        var isBodyweight = string.Equals(definition.Equipment.Trim(), "Bodyweight", StringComparison.OrdinalIgnoreCase);
        double? seededWeight = null;
        if (!isBodyweight)
        {
            var profile = await estimatedStrength.GetProfileAsync(userId);
            var oneRepMax = profile.GetOneRepMax(definition.Name);
            seededWeight = oneRepMax is > 0
                ? weightCalculator.RoundToNearest5(oneRepMax.Value / (1 + exercise.RepsMax / 30.0))
                : await MatchedAccessoryWeightAsync(definition.Name, exercise.RepsMin, exercise.RepsMax);
        }

        ApplySwap(exercise, definition.Name, definition.Id, definition.PrimaryMuscle, isBodyweight, seededWeight, scope);
        await db.SaveChangesAsync();
    }

    public async Task<List<PplAccessorySwapOption>> GetAccessorySwapOptionsAsync(int sessionExerciseId)
    {
        var userId = await userContext.GetUserIdAsync();
        var exercise = await db.PplSessionExercises.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == sessionExerciseId && e.Session.Program.UserId == userId);
        if (exercise is null) return [];

        var latest = (await accessoryService.GetLatestWeightsAsync()).ToDictionary(w => w.AccessoryId);
        return (await accessoryService.GetAllAccessoriesAsync())
            .Where(a => a.Category != AccessoryCategory.Cardio
                && !string.Equals(a.Name, exercise.ExerciseName, StringComparison.OrdinalIgnoreCase))
            .Select(a =>
            {
                latest.TryGetValue(a.Id, out var last);
                var likely = ExerciseNameMatcher.Score(exercise.ExerciseName, a.Name) >= ExerciseNameMatcher.SuggestThreshold;
                return new PplAccessorySwapOption(a.Id, a.Name, last?.Weight, last?.Reps, last?.LastUsed, likely);
            })
            .OrderByDescending(o => o.LikelyMatch)
            .ThenByDescending(o => o.LastUsed.HasValue)
            .ThenByDescending(o => o.LastUsed)
            .ThenBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SwapToAccessoryAsync(int sessionExerciseId, int accessoryId, PplSwapScope scope)
    {
        var userId = await userContext.GetUserIdAsync();
        var exercise = await LoadSwappableExerciseAsync(sessionExerciseId, userId);

        var accessory = await accessoryService.GetAccessoryAsync(accessoryId);
        if (accessory is null || !accessory.IsActive || accessory.Category == AccessoryCategory.Cardio)
            throw new InvalidOperationException("The selected accessory is unavailable.");

        // Start from the last weight actually used for this accessory.
        var last = (await accessoryService.GetLatestWeightsAsync()).FirstOrDefault(w => w.AccessoryId == accessory.Id);
        ApplySwap(exercise, accessory.Name.Trim(), null, exercise.ExerciseSlot.MuscleGroup, false, last?.Weight, scope);
        await db.SaveChangesAsync();
    }

    private async Task<PplSessionExercise> LoadSwappableExerciseAsync(int sessionExerciseId, string userId)
    {
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
        return exercise;
    }

    private async Task<double?> MatchedAccessoryWeightAsync(string exerciseName, int repsMin, int repsMax)
    {
        var best = (await accessoryService.GetLatestWeightsAsync())
            .Select(w => (Option: w, Score: ExerciseNameMatcher.Score(exerciseName, w.Name)))
            .Where(m => m.Score >= ExerciseNameMatcher.AutoMatchThreshold)
            .OrderByDescending(m => m.Score)
            .ThenByDescending(m => m.Option.LastUsed)
            .Select(m => m.Option)
            .FirstOrDefault();
        return best?.WeightForRepRange(repsMin, repsMax);
    }

    private static void ApplySwap(PplSessionExercise exercise, string name, int? definitionId, MuscleGroup muscle,
        bool isBodyweight, double? weight, PplSwapScope scope)
    {
        if (scope == PplSwapScope.FutureWorkouts)
        {
            var slot = exercise.ExerciseSlot;
            slot.ExerciseName = name;
            slot.ExerciseDefinitionId = definitionId;
            slot.MuscleGroup = muscle;
            slot.IsBodyweight = isBodyweight;
            slot.StartingWeight = weight;
            slot.CurrentWeight = weight;
            slot.BarId = null;
            if (isBodyweight) slot.ProgressionIncrement = 0;
            else if (slot.ProgressionIncrement <= 0) slot.ProgressionIncrement = 2.5;
        }

        exercise.ExerciseName = name;
        exercise.SuggestedWeight = weight;
        // A different movement usually means a different bar; fall back to the default.
        exercise.BarId = null;
        foreach (var set in exercise.Sets)
        {
            set.ActualWeight = null;
            set.ActualReps = null;
        }
    }
}