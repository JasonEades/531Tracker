using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IWorkoutService
{
    Task<Workout?> GetWorkoutWithDetailsAsync(int workoutId);
    Task UpdateSetAsync(int setId, double? actualWeight, int? actualReps, bool isCompleted);
    Task<WorkoutSet?> AddAdditionalSetAsync(int workoutId, double weight, int reps, AdditionalSetType type, double? rpe, double? rir, string? notes);
    Task UpdateAdditionalSetAsync(int setId, double weight, int reps, AdditionalSetType type, double? rpe, double? rir, string? notes);
    Task DeleteAdditionalSetAsync(int setId);
    Task StartWorkoutAsync(int workoutId);
    Task CompleteWorkoutAsync(int workoutId);
    Task ReopenWorkoutAsync(int workoutId);
    Task<Workout?> GetNextIncompleteWorkoutAsync();
    Task UpdateWorkoutBarAsync(int workoutId, int? barId);
    Task UpdateWorkoutNotesAsync(int workoutId, string? notes);
    Task UpdateWorkoutDateAsync(int workoutId, DateTime occurredOn);
}

public class WorkoutService(AppDbContext db, ICurrentUserService userContext) : IWorkoutService
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
            .Include(w => w.Bar)
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

    public async Task UpdateWorkoutDateAsync(int workoutId, DateTime occurredOn)
    {
        var workout = await GetOwnedWorkoutAsync(workoutId);
        if (workout is null) return;

        workout.OccurredOn = DateTime.SpecifyKind(occurredOn.Date, DateTimeKind.Utc);
        await db.SaveChangesAsync();
    }

    public async Task<WorkoutSet?> AddAdditionalSetAsync(int workoutId, double weight, int reps, AdditionalSetType type, double? rpe, double? rir, string? notes)
    {
        ValidateAdditionalSet(weight, reps, rpe, rir);

        var workout = await GetOwnedWorkoutAsync(workoutId);
        var primaryLiftId = workout?.Sets
            .Where(s => !s.IsAdditional && s.SetType == SetType.Main && s.Lift.LiftType == workout.MainLiftType)
            .Select(s => (int?)s.LiftId)
            .FirstOrDefault();
        if (workout is null || primaryLiftId is null) return null;

        var sequence = workout.Sets
            .Where(s => s.IsAdditional && s.LiftId == primaryLiftId.Value)
            .Select(s => (int?)s.Sequence)
            .Max() ?? 0;
        var set = new WorkoutSet
        {
            WorkoutId = workoutId,
            LiftId = primaryLiftId.Value,
            SetType = SetType.Main,
            SetNumber = sequence + 1,
            Sequence = sequence + 1,
            IsAdditional = true,
            AdditionalSetType = type,
            PrescribedWeight = weight,
            PrescribedReps = reps,
            ActualWeight = weight,
            ActualReps = reps,
            Rpe = rpe,
            Rir = rir,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsCompleted = true
        };

        db.WorkoutSets.Add(set);
        await db.SaveChangesAsync();
        return set;
    }

    public async Task UpdateAdditionalSetAsync(int setId, double weight, int reps, AdditionalSetType type, double? rpe, double? rir, string? notes)
    {
        ValidateAdditionalSet(weight, reps, rpe, rir);
        var set = await GetOwnedAdditionalSetAsync(setId);
        if (set is null) return;

        set.PrescribedWeight = weight;
        set.PrescribedReps = reps;
        set.ActualWeight = weight;
        set.ActualReps = reps;
        set.AdditionalSetType = type;
        set.Rpe = rpe;
        set.Rir = rir;
        set.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        await db.SaveChangesAsync();
    }

    public async Task DeleteAdditionalSetAsync(int setId)
    {
        var set = await GetOwnedAdditionalSetAsync(setId);
        if (set is null) return;

        db.WorkoutSets.Remove(set);
        await db.SaveChangesAsync();
    }

    public async Task UpdateWorkoutNotesAsync(int workoutId, string? notes)
    {
        var workout = await db.Workouts.FindAsync(workoutId);
        if (workout is null) return;

        workout.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
        await db.SaveChangesAsync();
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
        var userId = await userContext.GetUserIdAsync();
        return await db.Workouts
            .Include(w => w.Week)
                .ThenInclude(wk => wk.Cycle)
            .Where(w => w.Status != WorkoutStatus.Completed
                     && !w.Week.Cycle.IsCompleted
                     && w.Week.Cycle.UserId == userId)
            .OrderBy(w => w.Week.Cycle.CycleNumber)
            .ThenBy(w => w.Week.WeekNumber)
            .ThenBy(w => w.MainLiftType)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateWorkoutBarAsync(int workoutId, int? barId)
    {
        var workout = await db.Workouts.FindAsync(workoutId);
        if (workout is null) return;

        if (barId.HasValue)
        {
            var userId = await userContext.GetUserIdAsync();
            var barOwnedByUser = await db.Bars.AnyAsync(b => b.Id == barId.Value && b.UserId == userId);
            if (!barOwnedByUser) return;
        }

        workout.BarId = barId;
        await db.SaveChangesAsync();
    }

    private async Task<Workout?> GetOwnedWorkoutAsync(int workoutId)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Workouts
            .Include(w => w.Week).ThenInclude(w => w.Cycle)
            .Include(w => w.Sets).ThenInclude(s => s.Lift)
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.Week.Cycle.UserId == userId);
    }

    private async Task<WorkoutSet?> GetOwnedAdditionalSetAsync(int setId)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.WorkoutSets
            .Include(s => s.Workout).ThenInclude(w => w.Week).ThenInclude(w => w.Cycle)
            .FirstOrDefaultAsync(s => s.Id == setId && s.IsAdditional && s.Workout.Week.Cycle.UserId == userId);
    }

    private static void ValidateAdditionalSet(double weight, int reps, double? rpe, double? rir)
    {
        if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
        if (reps < 1) throw new ArgumentOutOfRangeException(nameof(reps));
        if (rpe is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(rpe));
        if (rir is < 0) throw new ArgumentOutOfRangeException(nameof(rir));
    }
}
