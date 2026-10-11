using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IEstimatedStrengthService
{
    Task<EstimatedStrengthProfile> GetProfileAsync(string userId);
}

public sealed class EstimatedStrengthProfile(
    IReadOnlyDictionary<LiftType, double> mainLiftOneRepMaxes,
    IReadOnlyDictionary<string, double> exerciseOneRepMaxes)
{
    private static readonly IReadOnlyDictionary<string, LiftType> MainLiftNames =
        new Dictionary<string, LiftType>(StringComparer.OrdinalIgnoreCase)
        {
            ["Squat"] = LiftType.Squat,
            ["Back Squat"] = LiftType.Squat,
            ["Bench Press"] = LiftType.BenchPress,
            ["Barbell Bench Press"] = LiftType.BenchPress,
            ["Deadlift"] = LiftType.Deadlift,
            ["Conventional Deadlift"] = LiftType.Deadlift,
            ["Overhead Press"] = LiftType.OverheadPress,
            ["Barbell Overhead Press"] = LiftType.OverheadPress
        };

    public static LiftType? MainLiftNameType(string exerciseName) =>
        MainLiftNames.TryGetValue(exerciseName, out var liftType) ? liftType : null;

    public double? GetOneRepMax(string exerciseName)
    {
        var estimates = new List<double>();
        if (exerciseOneRepMaxes.TryGetValue(exerciseName, out var exerciseEstimate))
            estimates.Add(exerciseEstimate);
        if (MainLiftNames.TryGetValue(exerciseName, out var liftType)
            && mainLiftOneRepMaxes.TryGetValue(liftType, out var mainLiftEstimate))
            estimates.Add(mainLiftEstimate);

        return estimates.Count == 0 ? null : estimates.Max();
    }

    public IReadOnlyDictionary<LiftType, double> MainLiftOneRepMaxes => mainLiftOneRepMaxes;
}

public sealed class EstimatedStrengthService(AppDbContext db, IWeightCalculator weightCalculator)
    : IEstimatedStrengthService
{
    public async Task<EstimatedStrengthProfile> GetProfileAsync(string userId)
    {
        var mainLiftSets = await db.WorkoutSets.AsNoTracking()
            .Where(set => set.Workout.Status == WorkoutStatus.Completed
                && set.Workout.Week.Cycle.UserId == userId
                && set.SetType == SetType.Main
                && set.IsCompleted
                && set.ActualWeight > 0
                && set.ActualReps > 0)
            .Select(set => new
            {
                set.Workout.MainLiftType,
                Weight = set.ActualWeight!.Value,
                Reps = set.ActualReps!.Value
            })
            .ToListAsync();

        var pplSets = await db.PplSessionSets.AsNoTracking()
            .Where(set => set.SessionExercise.Session.Status == WorkoutStatus.Completed
                && set.SessionExercise.Session.Program.UserId == userId
                && set.IsCompleted
                && set.ActualWeight > 0
                && set.ActualReps > 0)
            .Select(set => new
            {
                set.SessionExercise.ExerciseName,
                Weight = set.ActualWeight!.Value,
                Reps = set.ActualReps!.Value
            })
            .ToListAsync();

        var mainLiftEstimates = mainLiftSets
            .Select(set => (set.MainLiftType, Estimate: weightCalculator.CalculateEstimated1RM(set.Weight, set.Reps)))
            .Concat(pplSets
                .Where(set => EstimatedStrengthProfile.MainLiftNameType(set.ExerciseName).HasValue)
                .Select(set => (EstimatedStrengthProfile.MainLiftNameType(set.ExerciseName)!.Value,
                    Estimate: weightCalculator.CalculateEstimated1RM(set.Weight, set.Reps))))
            .Where(set => set.Estimate > 0)
            .GroupBy(set => set.Item1)
            .ToDictionary(group => group.Key, group => group.Max(set => set.Estimate));

        var exerciseEstimates = pplSets
            .Select(set => (set.ExerciseName, Estimate: weightCalculator.CalculateEstimated1RM(set.Weight, set.Reps)))
            .Where(set => set.Estimate > 0)
            .GroupBy(set => set.ExerciseName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Max(set => set.Estimate), StringComparer.OrdinalIgnoreCase);

        return new EstimatedStrengthProfile(mainLiftEstimates, exerciseEstimates);
    }
}
