using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface ILiftService
{
    Task<List<Lift>> GetAllLiftsAsync();
    Task<Lift?> GetLiftAsync(int id);
    Task<Lift?> GetLiftByTypeAsync(LiftType liftType);
    Task UpdateTrainingMaxAsync(int liftId, double newMax);
    Task UpdateBbbPercentageAsync(int liftId, double percentage);
}

public class LiftService(AppDbContext db) : ILiftService
{
    public async Task<List<Lift>> GetAllLiftsAsync()
    {
        return await db.Lifts.OrderBy(l => l.LiftType).ToListAsync();
    }

    public async Task<Lift?> GetLiftAsync(int id)
    {
        return await db.Lifts.FindAsync(id);
    }

    public async Task<Lift?> GetLiftByTypeAsync(LiftType liftType)
    {
        return await db.Lifts.FirstOrDefaultAsync(l => l.LiftType == liftType);
    }

    public async Task UpdateTrainingMaxAsync(int liftId, double newMax)
    {
        var lift = await db.Lifts.FindAsync(liftId);
        if (lift is not null)
        {
            lift.TrainingMax = newMax;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateBbbPercentageAsync(int liftId, double percentage)
    {
        var lift = await db.Lifts.FindAsync(liftId);
        if (lift is not null)
        {
            lift.BbbPercentage = Math.Clamp(percentage, 30, 70);
            await db.SaveChangesAsync();
        }
    }
}
