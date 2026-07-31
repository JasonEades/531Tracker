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
    Task<List<PplSession>> GetSessionHistoryAsync(int programId, int take = 30);
}

public class PplSessionService(AppDbContext db) : IPplSessionService
{
    public async Task<PplSession?> GetSessionWithDetailsAsync(int sessionId)
        => await db.PplSessions
            .Include(s => s.Program)
            .Include(s => s.DayTemplate)
            .Include(s => s.Exercises.OrderBy(e => e.OrderInSession))
                .ThenInclude(e => e.ExerciseSlot)
                    .ThenInclude(s => s.Lift)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets.OrderBy(s => s.SetNumber))
            .FirstOrDefaultAsync(s => s.Id == sessionId);

    public async Task StartSessionAsync(int sessionId)
    {
        var session = await db.PplSessions.FindAsync(sessionId);
        if (session is not null && session.Status == WorkoutStatus.NotStarted)
        {
            session.Status = WorkoutStatus.InProgress;
            session.StartedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task CompleteSessionAsync(int sessionId)
    {
        var session = await db.PplSessions.FindAsync(sessionId);
        if (session is not null)
        {
            session.Status = WorkoutStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task ReopenSessionAsync(int sessionId)
    {
        var session = await db.PplSessions.FindAsync(sessionId);
        if (session is not null && session.Status == WorkoutStatus.Completed)
        {
            session.Status = WorkoutStatus.InProgress;
            session.CompletedAt = null;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateSetAsync(int setId, double? actualWeight, int? actualReps, bool isCompleted)
    {
        var set = await db.PplSessionSets.FindAsync(setId);
        if (set is not null)
        {
            set.ActualWeight = actualWeight;
            set.ActualReps = actualReps;
            set.IsCompleted = isCompleted;
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<PplSession>> GetSessionHistoryAsync(int programId, int take = 30)
        => await db.PplSessions
            .Include(s => s.DayTemplate)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets)
            .Where(s => s.PplProgramId == programId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .ToListAsync();
}
