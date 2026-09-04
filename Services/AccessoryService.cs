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
    Task<WorkoutAccessory> AddAccessoryToWorkoutAsync(int workoutId, int accessoryId, double weight, int reps, int sets);
    Task UpdateWorkoutAccessoryAsync(int id, double weight, int reps, int sets, bool isCompleted);
    Task RemoveWorkoutAccessoryAsync(int id);
    Task<AccessoryHistory?> GetSuggestedUsageAsync(int accessoryId);
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

    public async Task<WorkoutAccessory> AddAccessoryToWorkoutAsync(int workoutId, int accessoryId, double weight, int reps, int sets)
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
            Sets = sets
        };
        db.WorkoutAccessories.Add(wa);
        await db.SaveChangesAsync();

        await RecordAccessoryHistoryAsync(accessoryId, weight, reps, sets);

        return wa;
    }

    public async Task UpdateWorkoutAccessoryAsync(int id, double weight, int reps, int sets, bool isCompleted)
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

    public async Task<AccessoryHistory?> GetSuggestedUsageAsync(int accessoryId)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.AccessoryHistory
            .Where(h => h.AccessoryId == accessoryId && h.UserId == userId)
            .OrderByDescending(h => h.RecordedAt)
            .FirstOrDefaultAsync();
    }

    public async Task RecordAccessoryHistoryAsync(int accessoryId, double weight, int reps, int sets)
    {
        var userId = await userContext.GetUserIdAsync();
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
