using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IAccessoryService
{
    Task<List<Accessory>> GetAllAccessoriesAsync();
    Task<Accessory?> GetAccessoryAsync(int id);
    Task<Accessory> CreateAccessoryAsync(string name, string? description);
    Task UpdateAccessoryAsync(int id, string name, string? description, bool isActive);
    Task DeleteAccessoryAsync(int id);
    Task<WorkoutAccessory> AddAccessoryToWorkoutAsync(int workoutId, int accessoryId, double weight, int reps, int sets, string? notes);
    Task UpdateWorkoutAccessoryAsync(int id, double weight, int reps, int sets, string? notes, bool isCompleted);
    Task RemoveWorkoutAccessoryAsync(int id);
    Task<AccessoryUsageSuggestion?> GetSuggestedUsageAsync(int accessoryId);
    Task<AccessoryUsageSuggestion?> GetSuggestedUsageAsync(string optionKey);
    Task<List<AccessoryWeightOption>> GetLatestWeightsAsync();
    Task RecordAccessoryHistoryAsync(int accessoryId, double weight, int reps, int sets);
    Task<List<AccessoryExerciseOption>> GetExerciseOptionsAsync();
    Task<int> ResolveAccessoryIdAsync(string optionKey);
    Task LinkAccessoryAsync(int accessoryId, int exerciseDefinitionId);
}

public class AccessoryService(AppDbContext db, ICurrentUserService userContext) : IAccessoryService
{
    public async Task<List<Accessory>> GetAllAccessoriesAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Accessories
            .Where(a => a.IsActive && (a.UserId == null || a.UserId == userId))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Accessory?> GetAccessoryAsync(int id)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Accessories
            .FirstOrDefaultAsync(a => a.Id == id && (a.UserId == null || a.UserId == userId));
    }

    public async Task<Accessory> CreateAccessoryAsync(string name, string? description)
    {
        var userId = await userContext.GetUserIdAsync();
        var accessory = new Accessory
        {
            UserId = userId,
            Name = name,
            Description = description
        };
        db.Accessories.Add(accessory);
        await db.SaveChangesAsync();
        return accessory;
    }

    public async Task UpdateAccessoryAsync(int id, string name, string? description, bool isActive)
    {
        var userId = await userContext.GetUserIdAsync();
        var accessory = await db.Accessories
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (accessory is not null)
        {
            accessory.Name = name;
            accessory.Description = description;
            accessory.IsActive = isActive;
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteAccessoryAsync(int id)
    {
        var userId = await userContext.GetUserIdAsync();
        var accessory = await db.Accessories
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (accessory is not null)
        {
            accessory.IsActive = false;
            await db.SaveChangesAsync();
        }
    }

    public async Task<WorkoutAccessory> AddAccessoryToWorkoutAsync(int workoutId, int accessoryId, double weight, int reps, int sets, string? notes)
    {
        var userId = await userContext.GetUserIdAsync();
        var ownsWorkout = await db.Workouts
            .AnyAsync(w => w.Id == workoutId && w.Week.Cycle.UserId == userId);
        var canUseAccessory = await db.Accessories
            .AnyAsync(a => a.Id == accessoryId && a.IsActive && (a.UserId == null || a.UserId == userId));

        if (!ownsWorkout || !canUseAccessory)
            throw new InvalidOperationException("The workout or accessory is not available to the current user.");

        var wa = new WorkoutAccessory
        {
            WorkoutId = workoutId,
            AccessoryId = accessoryId,
            Weight = weight,
            Reps = reps,
            Sets = sets,
            Notes = NormalizeNotes(notes)
        };
        db.WorkoutAccessories.Add(wa);
        await db.SaveChangesAsync();

        await RecordAccessoryHistoryAsync(accessoryId, weight, reps, sets);

        return wa;
    }

    public async Task UpdateWorkoutAccessoryAsync(int id, double weight, int reps, int sets, string? notes, bool isCompleted)
    {
        var userId = await userContext.GetUserIdAsync();
        var wa = await db.WorkoutAccessories
            .Include(w => w.Accessory)
            .Include(w => w.Workout)
                .ThenInclude(w => w.Week)
                    .ThenInclude(w => w.Cycle)
            .FirstOrDefaultAsync(w => w.Id == id
                && w.Workout.Week.Cycle.UserId == userId
                && (w.Accessory.UserId == null || w.Accessory.UserId == userId));
        if (wa is not null)
        {
            wa.Weight = weight;
            wa.Reps = reps;
            wa.Sets = sets;
            wa.Notes = NormalizeNotes(notes);
            wa.IsCompleted = isCompleted;
            await db.SaveChangesAsync();

            if (isCompleted)
            {
                await RecordAccessoryHistoryAsync(wa.AccessoryId, weight, reps, sets);
            }
        }
    }

    public async Task RemoveWorkoutAccessoryAsync(int id)
    {
        var userId = await userContext.GetUserIdAsync();
        var wa = await db.WorkoutAccessories
            .Include(w => w.Workout)
                .ThenInclude(w => w.Week)
                    .ThenInclude(w => w.Cycle)
            .FirstOrDefaultAsync(w => w.Id == id && w.Workout.Week.Cycle.UserId == userId);
        if (wa is not null)
        {
            db.WorkoutAccessories.Remove(wa);
            await db.SaveChangesAsync();
        }
    }

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    public async Task<AccessoryUsageSuggestion?> GetSuggestedUsageAsync(int accessoryId)
    {
        var userId = await userContext.GetUserIdAsync();
        var accessoryIds = await SameExerciseAccessoryIdsAsync(accessoryId, userId);
        var logged = await db.WorkoutAccessories
            .AsNoTracking()
            .Where(w => accessoryIds.Contains(w.AccessoryId) && w.Workout.Week.Cycle.UserId == userId)
            .OrderByDescending(w => w.Workout.OccurredOn)
            .ThenByDescending(w => w.Id)
            .Select(w => new AccessoryUsageSuggestion
            {
                Weight = w.Weight,
                Reps = w.Reps,
                Sets = w.Sets,
                Notes = w.Notes,
                LastPerformed = w.Workout.OccurredOn
            })
            .FirstOrDefaultAsync();
        if (logged is not null) return logged;

        var accessory = await db.Accessories.AsNoTracking()
            .Where(a => a.Id == accessoryId && (a.UserId == null || a.UserId == userId) && a.Category != AccessoryCategory.Cardio)
            .Select(a => new { Name = a.ExerciseDefinition != null ? a.ExerciseDefinition.Name : a.Name })
            .FirstOrDefaultAsync();
        return accessory is null ? null : await SuggestFromOtherProgramsAsync(accessory.Name, userId);
    }

    /// <summary>Suggestion for a picker key; library exercises never logged as accessories use PPL/other history.</summary>
    public async Task<AccessoryUsageSuggestion?> GetSuggestedUsageAsync(string optionKey)
    {
        if (optionKey.StartsWith("a:", StringComparison.Ordinal) && int.TryParse(optionKey[2..], out var accessoryId))
            return await GetSuggestedUsageAsync(accessoryId);
        if (!optionKey.StartsWith("d:", StringComparison.Ordinal) || !int.TryParse(optionKey[2..], out var definitionId))
            return null;

        var userId = await userContext.GetUserIdAsync();
        var name = await db.ExerciseDefinitions.AsNoTracking()
            .Where(d => d.Id == definitionId && (d.UserId == null || d.UserId == userId))
            .Select(d => d.Name)
            .FirstOrDefaultAsync();
        return name is null ? null : await SuggestFromOtherProgramsAsync(name, userId);
    }

    private async Task<AccessoryUsageSuggestion?> SuggestFromOtherProgramsAsync(string exerciseName, string userId)
    {
        var observations = await ExerciseObservations.LoadAsync(db, userId, new WeightCalculator());
        var latest = ExerciseObservations.Latest(observations, exerciseName);
        if (latest is null) return null;

        var top = latest.TopSet;
        return new AccessoryUsageSuggestion
        {
            Weight = top.Weight,
            Reps = top.Reps,
            Sets = latest.Sets.Count(s => s.Weight == top.Weight),
            LastPerformed = latest.Date,
            Source = latest.Source
        };
    }

    public async Task<List<AccessoryWeightOption>> GetLatestWeightsAsync()
    {
        var userId = await userContext.GetUserIdAsync();

        var workoutRows = await db.WorkoutAccessories.AsNoTracking()
            .Where(w => w.Weight > 0 && w.Reps > 0
                && w.Workout.Week.Cycle.UserId == userId
                && (w.Accessory.UserId == null || w.Accessory.UserId == userId)
                && w.Accessory.Category != AccessoryCategory.Cardio)
            .Select(w => new { w.AccessoryId, w.Accessory.ExerciseDefinitionId, Name = w.Accessory.ExerciseDefinition != null ? w.Accessory.ExerciseDefinition.Name : w.Accessory.Name, w.Weight, w.Reps, Date = w.Workout.OccurredOn, w.Id })
            .ToListAsync();

        var historyRows = await db.AccessoryHistory.AsNoTracking()
            .Where(h => h.Weight > 0 && h.Reps > 0 && h.UserId == userId
                && (h.Accessory.UserId == null || h.Accessory.UserId == userId)
                && h.Accessory.Category != AccessoryCategory.Cardio)
            .Select(h => new { h.AccessoryId, h.Accessory.ExerciseDefinitionId, Name = h.Accessory.ExerciseDefinition != null ? h.Accessory.ExerciseDefinition.Name : h.Accessory.Name, h.Weight, h.Reps, Date = h.RecordedAt, h.Id })
            .ToListAsync();

        // Accessories linked to the same library exercise are one exercise.
        return workoutRows.Concat(historyRows)
            .GroupBy(r => r.ExerciseDefinitionId is int definitionId ? $"d:{definitionId}" : $"a:{r.AccessoryId}")
            .Select(g => g.OrderByDescending(r => r.Date).ThenByDescending(r => r.Id).First())
            .Select(r => new AccessoryWeightOption(r.AccessoryId, r.Name, r.Weight, r.Reps, r.Date))
            .OrderBy(o => o.Name)
            .ToList();
    }

    public async Task RecordAccessoryHistoryAsync(int accessoryId, double weight, int reps, int sets)
    {
        var userId = await userContext.GetUserIdAsync();
        var canRecordHistory = await db.Accessories
            .AnyAsync(a => a.Id == accessoryId && (a.UserId == null || a.UserId == userId));
        if (!canRecordHistory)
            throw new InvalidOperationException("The accessory is not available to the current user.");

        db.AccessoryHistory.Add(new AccessoryHistory
        {
            AccessoryId = accessoryId,
            UserId = userId,
            Weight = weight,
            Reps = reps,
            Sets = sets,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<AccessoryExerciseOption>> GetExerciseOptionsAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        var accessories = await db.Accessories.AsNoTracking()
            .Where(a => a.IsActive && (a.UserId == null || a.UserId == userId))
            .Select(a => new { a.Id, a.Name, a.UserId, a.ExerciseDefinitionId, a.Category })
            .ToListAsync();
        var definitions = await db.ExerciseDefinitions.AsNoTracking()
            .Where(d => d.UserId == null || d.UserId == userId)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();

        var accessoryByDefinition = accessories
            .Where(a => a.ExerciseDefinitionId.HasValue)
            .GroupBy(a => a.ExerciseDefinitionId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.UserId == null ? 0 : 1).ThenBy(a => a.Id).First());

        return definitions
            .Select(d => accessoryByDefinition.TryGetValue(d.Id, out var accessory)
                ? new AccessoryExerciseOption($"a:{accessory.Id}", d.Name, accessory.Category == AccessoryCategory.Cardio)
                : new AccessoryExerciseOption($"d:{d.Id}", d.Name, false))
            .Concat(accessories
                .Where(a => a.ExerciseDefinitionId is null)
                .Select(a => new AccessoryExerciseOption($"a:{a.Id}", a.Name, a.Category == AccessoryCategory.Cardio)))
            .OrderBy(o => o.IsCardio)
            .ThenBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Turns a picker key into an accessory id, creating the user's accessory for a library exercise on first use.</summary>
    public async Task<int> ResolveAccessoryIdAsync(string optionKey)
    {
        var userId = await userContext.GetUserIdAsync();
        if (optionKey.StartsWith("a:", StringComparison.Ordinal) && int.TryParse(optionKey[2..], out var accessoryId))
        {
            if (await db.Accessories.AnyAsync(a => a.Id == accessoryId && a.IsActive && (a.UserId == null || a.UserId == userId)))
                return accessoryId;
        }
        else if (optionKey.StartsWith("d:", StringComparison.Ordinal) && int.TryParse(optionKey[2..], out var definitionId))
        {
            var definition = await db.ExerciseDefinitions
                .FirstOrDefaultAsync(d => d.Id == definitionId && (d.UserId == null || d.UserId == userId));
            if (definition is not null)
            {
                var existing = await db.Accessories
                    .Where(a => a.ExerciseDefinitionId == definitionId && (a.UserId == null || a.UserId == userId))
                    .OrderByDescending(a => a.IsActive)
                    .ThenBy(a => a.UserId == null ? 0 : 1)
                    .ThenBy(a => a.Id)
                    .FirstOrDefaultAsync();
                if (existing is not null)
                {
                    if (!existing.IsActive && existing.UserId == userId)
                    {
                        existing.IsActive = true;
                        await db.SaveChangesAsync();
                    }
                    if (existing.IsActive) return existing.Id;
                }

                var accessory = new Accessory
                {
                    UserId = userId,
                    Name = definition.Name,
                    Category = AccessoryCategory.Strength,
                    ExerciseDefinitionId = definition.Id
                };
                db.Accessories.Add(accessory);
                await db.SaveChangesAsync();
                return accessory.Id;
            }
        }

        throw new InvalidOperationException("The selected exercise is not available to the current user.");
    }

    /// <summary>Merges one of the user's own exercises into a library entry; logged history stays on the accessory.</summary>
    public async Task LinkAccessoryAsync(int accessoryId, int exerciseDefinitionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var accessory = await db.Accessories.FirstOrDefaultAsync(a => a.Id == accessoryId && a.UserId == userId)
            ?? throw new InvalidOperationException("Only your own exercises can be linked.");
        if (!await db.ExerciseDefinitions.AnyAsync(d => d.Id == exerciseDefinitionId && (d.UserId == null || d.UserId == userId)))
            throw new InvalidOperationException("The selected library exercise is not available.");

        accessory.ExerciseDefinitionId = exerciseDefinitionId;
        await db.SaveChangesAsync();
    }

    private async Task<List<int>> SameExerciseAccessoryIdsAsync(int accessoryId, string userId)
    {
        var definitionId = await db.Accessories
            .Where(a => a.Id == accessoryId)
            .Select(a => a.ExerciseDefinitionId)
            .FirstOrDefaultAsync();
        if (definitionId is null) return [accessoryId];

        return await db.Accessories
            .Where(a => a.ExerciseDefinitionId == definitionId && (a.UserId == null || a.UserId == userId))
            .Select(a => a.Id)
            .ToListAsync();
    }
}
