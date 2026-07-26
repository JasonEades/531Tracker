using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IWorkoutService
{
    Task<Workout?> GetWorkoutWithDetailsAsync(int workoutId);
    Task UpdateSetAsync(int setId, double? actualWeight, int? actualReps, bool isCompleted);
    Task StartWorkoutAsync(int workoutId);
    Task CompleteWorkoutAsync(int workoutId);
    Task ReopenWorkoutAsync(int workoutId);
    Task<Workout?> GetNextIncompleteWorkoutAsync();
}

public class WorkoutService(AppDbContext db) : IWorkoutService
{
    public async Task<Workout?> GetWorkoutWithDetailsAsync(int workoutId)
    {
        return await db.Workouts
            .Include(w => w.Week)
                .ThenInclude(wk => wk.Cycle)
            .Include(w => w.Sets)
                .ThenInclude(s => s.Lift)
            .Include(w => w.WorkoutAccessories)
                .ThenInclude(wa => wa.Accessory)
            .FirstOrDefaultAsync(w => w.Id == workoutId);
    }

    public async Task UpdateSetAsync(int setId, double? actualWeight, int? actualReps, bool isCompleted)
    {
        var set = await db.WorkoutSets.FindAsync(setId);
        if (set is not null)
        {
            set.ActualWeight = actualWeight;
            set.ActualReps = actualReps;
            set.IsCompleted = isCompleted;
            await db.SaveChangesAsync();
        }
    }

    public async Task StartWorkoutAsync(int workoutId)
    {
        var workout = await db.Workouts.FindAsync(workoutId);
        if (workout is not null && workout.Status == WorkoutStatus.NotStarted)
        {
            workout.Status = WorkoutStatus.InProgress;
            await db.SaveChangesAsync();
        }
    }

    public async Task CompleteWorkoutAsync(int workoutId)
    {
        var workout = await db.Workouts.FindAsync(workoutId);
        if (workout is not null)
        {
            workout.Status = WorkoutStatus.Completed;
            workout.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task ReopenWorkoutAsync(int workoutId)
    {
        var workout = await db.Workouts.FindAsync(workoutId);
        if (workout is not null && workout.Status == WorkoutStatus.Completed)
        {
            workout.Status = WorkoutStatus.InProgress;
            workout.CompletedAt = null;
            await db.SaveChangesAsync();
        }
    }

    public async Task<Workout?> GetNextIncompleteWorkoutAsync()
    {
        return await db.Workouts
            .Include(w => w.Week)
                .ThenInclude(wk => wk.Cycle)
            .Where(w => w.Status != WorkoutStatus.Completed && !w.Week.Cycle.IsCompleted)
            .OrderBy(w => w.Week.Cycle.CycleNumber)
            .ThenBy(w => w.Week.WeekNumber)
            .ThenBy(w => w.MainLiftType)
            .FirstOrDefaultAsync();
    }
}
