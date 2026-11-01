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

    /// <summary>Library exercises and generic accessories that can be added to the session (not already in it).</summary>
    Task<PplAddExerciseOptions> GetAddExerciseOptionsAsync(int sessionId);

    /// <summary>
    /// Adds an exercise to this session only (e.g. ab work on leg day); the plan is unchanged. The suggested
    /// weight is the final weight last logged for the exercise anywhere, otherwise an estimate.
    /// </summary>
    Task<PplSessionExercise> AddExerciseAsync(int sessionId, PplAddExerciseRequest request);

    /// <summary>Removes an exercise that was added to this session (plan exercises can't be removed).</summary>
    Task RemoveAddedExerciseAsync(int sessionExerciseId);
}

public enum PplAddExerciseSource { Library, Accessory }

public sealed record PplAddExerciseRequest(PplAddExerciseSource Source, int SourceId, int Sets = 3, int RepsMin = 10, int RepsMax = 15);

public sealed record PplAddExerciseOptions(List<ExerciseDefinition> Exercises, List<PplAccessorySwapOption> Accessories);

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
            .Include(e => e.ExerciseSlot).ThenInclude(s => s!.ExerciseDefinition)
            .FirstOrDefaultAsync(e => e.Id == sessionExerciseId && e.Session.Program.UserId == userId);
        if (exercise?.ExerciseSlot is null) return [];

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
        ApplySwap(exercise, accessory.Name.Trim(), null, exercise.ExerciseSlot!.MuscleGroup, false, last?.Weight, scope);
        await db.SaveChangesAsync();
    }

    public async Task<PplAddExerciseOptions> GetAddExerciseOptionsAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var existing = await db.PplSessionExercises.AsNoTracking()
            .Where(e => e.PplSessionId == sessionId && e.Session.Program.UserId == userId)
            .Select(e => e.ExerciseName)
            .ToListAsync();
        if (!await db.PplSessions.AnyAsync(s => s.Id == sessionId && s.Program.UserId == userId))
            return new PplAddExerciseOptions([], []);

        var taken = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var exercises = (await db.ExerciseDefinitions.AsNoTracking()
                .Where(d => d.UserId == null || d.UserId == userId)
                .ToListAsync())
            .Where(d => !taken.Contains(d.Name))
            .OrderBy(d => d.PrimaryMuscle)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var libraryNames = exercises.Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var latest = (await accessoryService.GetLatestWeightsAsync()).ToDictionary(w => w.AccessoryId);
        // Accessories that are library exercises are already listed above.
        var accessories = (await accessoryService.GetAllAccessoriesAsync())
            .Where(a => a.Category != AccessoryCategory.Cardio && a.ExerciseDefinitionId is null
                && !taken.Contains(a.Name) && !libraryNames.Contains(a.Name))
            .Select(a =>
            {
                latest.TryGetValue(a.Id, out var last);
                return new PplAccessorySwapOption(a.Id, a.Name, last?.Weight, last?.Reps, last?.LastUsed, false);
            })
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new PplAddExerciseOptions(exercises, accessories);
    }

    public async Task<PplSessionExercise> AddExerciseAsync(int sessionId, PplAddExerciseRequest request)
    {
        if (request.Sets is < 1 or > 10)
            throw new InvalidOperationException("Sets must be between 1 and 10.");
        if (request.RepsMin < 1 || request.RepsMax > PplSessionService.MaxReps || request.RepsMin > request.RepsMax)
            throw new InvalidOperationException($"Reps must be between 1 and {PplSessionService.MaxReps}, with the minimum no higher than the maximum.");

        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions
            .Include(s => s.Exercises)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId)
            ?? throw new InvalidOperationException("Session was not found.");
        if (session.Status == WorkoutStatus.Completed)
            throw new InvalidOperationException("Reopen the session before adding exercises.");

        string name;
        MuscleGroup? muscle;
        bool isBodyweight;
        double? fallbackWeight = null;
        ExerciseDefinition? definition;
        if (request.Source == PplAddExerciseSource.Library)
        {
            definition = await db.ExerciseDefinitions.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.SourceId && (d.UserId == null || d.UserId == userId))
                ?? throw new InvalidOperationException("The selected exercise is unavailable.");
            name = definition.Name;
        }
        else
        {
            var accessory = await accessoryService.GetAccessoryAsync(request.SourceId);
            if (accessory is null || !accessory.IsActive || accessory.Category == AccessoryCategory.Cardio)
                throw new InvalidOperationException("The selected accessory is unavailable.");
            definition = accessory.ExerciseDefinitionId is int definitionId
                ? await db.ExerciseDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == definitionId)
                : null;
            name = definition?.Name ?? accessory.Name.Trim();
            fallbackWeight = (await accessoryService.GetLatestWeightsAsync()).FirstOrDefault(w => w.AccessoryId == accessory.Id)?.Weight;
        }
        muscle = definition?.PrimaryMuscle;
        isBodyweight = definition is not null
            && string.Equals(definition.Equipment.Trim(), "Bodyweight", StringComparison.OrdinalIgnoreCase);

        if (session.Exercises.Any(e => string.Equals(e.ExerciseName, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"{name} is already in this session.");

        double? weight = null;
        if (!isBodyweight)
        {
            weight = await LastWorkingWeightAsync(userId, name, session.Id);
            if (weight is null && definition is not null && request.Source == PplAddExerciseSource.Library)
            {
                var profile = await estimatedStrength.GetProfileAsync(userId);
                var oneRepMax = profile.GetOneRepMax(definition.Name);
                weight = oneRepMax is > 0
                    ? weightCalculator.RoundToNearest5(oneRepMax.Value / (1 + request.RepsMax / 30.0))
                    : await MatchedAccessoryWeightAsync(definition.Name, request.RepsMin, request.RepsMax);
            }
            weight ??= fallbackWeight;
        }

        var exercise = new PplSessionExercise
        {
            PplSessionId = session.Id,
            ExerciseName = name,
            AddedMuscleGroup = muscle,
            AddedIsBodyweight = isBodyweight,
            TargetSets = request.Sets,
            RepsMin = request.RepsMin,
            RepsMax = request.RepsMax,
            SuggestedWeight = weight,
            OrderInSession = session.Exercises.Count == 0 ? 1 : session.Exercises.Max(e => e.OrderInSession) + 1
        };
        for (var i = 1; i <= request.Sets; i++)
            exercise.Sets.Add(new PplSessionSet { SetNumber = i, TargetReps = request.RepsMax });
        session.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        return exercise;
    }

    public async Task RemoveAddedExerciseAsync(int sessionExerciseId)
    {
        var userId = await userContext.GetUserIdAsync();
        var exercise = await db.PplSessionExercises
            .Include(e => e.Session)
            .FirstOrDefaultAsync(e => e.Id == sessionExerciseId && e.Session.Program.UserId == userId)
            ?? throw new InvalidOperationException("Exercise was not found.");
        if (exercise.PplExerciseSlotId is not null)
            throw new InvalidOperationException("Plan exercises can't be removed from a session.");
        if (exercise.Session.Status == WorkoutStatus.Completed)
            throw new InvalidOperationException("Reopen the session before removing exercises.");

        db.PplSessionExercises.Remove(exercise);
        await db.SaveChangesAsync();
    }

    /// <summary>The weight of the final set the last time this exercise was logged in any program (before this session).</summary>
    private async Task<double?> LastWorkingWeightAsync(string userId, string name, int currentSessionId)
    {
        var currentKey = $"ppl:s{currentSessionId}";
        var observations = (await ExerciseObservations.LoadAsync(db, userId, weightCalculator))
            .Where(o => o.SessionKey != currentKey);
        var latest = ExerciseObservations.Latest(observations, name);
        return latest?.Sets.LastOrDefault(s => s.Weight > 0)?.Weight;
    }

    private async Task<PplSessionExercise> LoadSwappableExerciseAsync(int sessionExerciseId, string userId)
    {
        var exercise = await db.PplSessionExercises
            .Include(e => e.Session)
            .Include(e => e.Sets)
            .Include(e => e.ExerciseSlot)
            .FirstOrDefaultAsync(e => e.Id == sessionExerciseId && e.Session.Program.UserId == userId)
            ?? throw new InvalidOperationException("Exercise was not found.");
        if (exercise.ExerciseSlot is null)
            throw new InvalidOperationException("Exercises added to a session can't be swapped; remove it and add another instead.");
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
            var slot = exercise.ExerciseSlot!;
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