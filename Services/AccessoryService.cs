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
    Task<double?> GetSuggestedWeightAsync(int accessoryId);
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
        return await db.Accessories.FindAsync(id);
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
        var accessory = await db.Accessories.FindAsync(id);
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
        var accessory = await db.Accessories.FindAsync(id);
        if (accessory is not null)
        {
            accessory.IsActive = false;
            await db.SaveChangesAsync();
        }
    }

    public async Task<WorkoutAccessory> AddAccessoryToWorkoutAsync(int workoutId, int accessoryId, double weight, int reps, int sets)
    {
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
        var wa = await db.WorkoutAccessories
            .Include(w => w.Accessory)
            .FirstOrDefaultAsync(w => w.Id == id);
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
        var wa = await db.WorkoutAccessories.FindAsync(id);
        if (wa is not null)
        {
            db.WorkoutAccessories.Remove(wa);
            await db.SaveChangesAsync();
        }
    }

    public async Task<double?> GetSuggestedWeightAsync(int accessoryId)
    {
        var lastEntry = await db.AccessoryHistory
            .Where(h => h.AccessoryId == accessoryId)
            .OrderByDescending(h => h.RecordedAt)
            .FirstOrDefaultAsync();

        return lastEntry?.Weight;
    }

    public async Task RecordAccessoryHistoryAsync(int accessoryId, double weight, int reps, int sets)
    {
        db.AccessoryHistory.Add(new AccessoryHistory
        {
            AccessoryId = accessoryId,
            Weight = weight,
            Reps = reps,
            Sets = sets,
            RecordedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
