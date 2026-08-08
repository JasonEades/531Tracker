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

public class LiftService(AppDbContext db, ICurrentUserService userContext) : ILiftService
{
    public async Task<List<Lift>> GetAllLiftsAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Lifts
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.LiftType)
            .ToListAsync();
    }

    public async Task<Lift?> GetLiftAsync(int id)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Lifts.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
    }

    public async Task<Lift?> GetLiftByTypeAsync(LiftType liftType)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Lifts.FirstOrDefaultAsync(l => l.LiftType == liftType && l.UserId == userId);
    }

    public async Task UpdateTrainingMaxAsync(int liftId, double newMax)
    {
        var userId = await userContext.GetUserIdAsync();
        var lift = await db.Lifts.FirstOrDefaultAsync(l => l.Id == liftId && l.UserId == userId);
        if (lift is not null)
        {
            lift.TrainingMax = newMax;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateBbbPercentageAsync(int liftId, double percentage)
    {
        var userId = await userContext.GetUserIdAsync();
        var lift = await db.Lifts.FirstOrDefaultAsync(l => l.Id == liftId && l.UserId == userId);
        if (lift is not null)
        {
            lift.BbbPercentage = Math.Clamp(percentage, 30, 70);
            await db.SaveChangesAsync();
        }
    }
}
