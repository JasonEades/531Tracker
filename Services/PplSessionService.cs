using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IPplSessionService
{
    Task<PplSession?> GetSessionWithDetailsAsync(int sessionId);
    Task StartSessionAsync(int sessionId);
    Task CompleteSessionAsync(int sessionId);
    Task ReopenSessionAsync(int sessionId);
    Task UpdateSetAsync(int setId, double? actualWeight, int? actualReps, bool isCompleted);
    Task SetStartingWeightAsync(int exerciseSlotId, double weight);
    Task<List<PplSession>> GetSessionHistoryAsync(int programId, int take = 30);
}

public class PplSessionService(AppDbContext db, ICurrentUserService userContext) : IPplSessionService
{
    public async Task<PplSession?> GetSessionWithDetailsAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.PplSessions
            .Include(s => s.Program)
            .Include(s => s.Week)
            .Include(s => s.DayTemplate)
            .Include(s => s.Week)
            .Include(s => s.Exercises.OrderBy(e => e.OrderInSession))
                .ThenInclude(e => e.ExerciseSlot)
                    .ThenInclude(s => s.Lift)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets.OrderBy(s => s.SetNumber))
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
    }

    public async Task StartSessionAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is not null && session.Status == WorkoutStatus.NotStarted)
        {
            session.Status = WorkoutStatus.InProgress;
            session.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task SetStartingWeightAsync(int exerciseSlotId, double weight)
    {
        if (weight <= 0) return;

        var userId = await userContext.GetUserIdAsync();
        var slot = await db.PplExerciseSlots
            .Include(s => s.DayTemplate)
            .ThenInclude(d => d.Program)
            .FirstOrDefaultAsync(s => s.Id == exerciseSlotId && s.DayTemplate.Program.UserId == userId);
        if (slot is not null)
        {
            slot.StartingWeight ??= weight;
            slot.CurrentWeight ??= weight;
            await db.SaveChangesAsync();
        }
    }

    public async Task CompleteSessionAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is not null)
        {
            session.Status = WorkoutStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task ReopenSessionAsync(int sessionId)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await db.PplSessions.Include(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.Program.UserId == userId);
        if (session is not null && session.Status == WorkoutStatus.Completed)
        {
            session.Status = WorkoutStatus.InProgress;
            session.CompletedAt = null;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateSetAsync(int setId, double? actualWeight, int? actualReps, bool isCompleted)
    {
        var userId = await userContext.GetUserIdAsync();
        var set = await db.PplSessionSets
            .Include(s => s.SessionExercise)
                .ThenInclude(e => e.Session)
                    .ThenInclude(s => s.Program)
            .FirstOrDefaultAsync(s => s.Id == setId && s.SessionExercise.Session.Program.UserId == userId);
        if (set is not null)
        {
            set.ActualWeight = actualWeight;
            set.ActualReps = actualReps;
            set.IsCompleted = isCompleted;
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<PplSession>> GetSessionHistoryAsync(int programId, int take = 30)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.PplSessions
            .Include(s => s.DayTemplate)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets)
            .Where(s => s.PplProgramId == programId && s.Program.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}
