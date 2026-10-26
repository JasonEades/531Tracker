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
    Task<List<AccessoryWeightOption>> GetLatestWeightsAsync();
    Task RecordAccessoryHistoryAsync(int accessoryId, double weight, int reps, int sets);
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
        return await db.WorkoutAccessories
            .AsNoTracking()
            .Where(w => w.AccessoryId == accessoryId && w.Workout.Week.Cycle.UserId == userId)
            .OrderByDescending(w => w.Workout.OccurredOn)
            .ThenByDescending(w => w.Id)
            .Select(w => new AccessoryUsageSuggestion
            {
                Weight = w.Weight,
                Reps = w.Reps,
                Sets = w.Sets,
                Notes = w.Notes
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<AccessoryWeightOption>> GetLatestWeightsAsync()
    {
        var userId = await userContext.GetUserIdAsync();

        var workoutRows = await db.WorkoutAccessories.AsNoTracking()
            .Where(w => w.Weight > 0 && w.Reps > 0
                && w.Workout.Week.Cycle.UserId == userId
                && (w.Accessory.UserId == null || w.Accessory.UserId == userId)
                && w.Accessory.Category != AccessoryCategory.Cardio)
            .Select(w => new { w.AccessoryId, w.Accessory.Name, w.Weight, w.Reps, Date = w.Workout.OccurredOn, w.Id })
            .ToListAsync();

        var historyRows = await db.AccessoryHistory.AsNoTracking()
            .Where(h => h.Weight > 0 && h.Reps > 0 && h.UserId == userId
                && (h.Accessory.UserId == null || h.Accessory.UserId == userId)
                && h.Accessory.Category != AccessoryCategory.Cardio)
            .Select(h => new { h.AccessoryId, h.Accessory.Name, h.Weight, h.Reps, Date = h.RecordedAt, h.Id })
            .ToListAsync();

        return workoutRows.Concat(historyRows)
            .GroupBy(r => r.AccessoryId)
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
}
