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
        // Main-lift estimates already pick the most recent source across 5/3/1 and PPL.
        if (MainLiftNames.TryGetValue(exerciseName, out var liftType)
            && mainLiftOneRepMaxes.TryGetValue(liftType, out var mainLiftEstimate))
            return mainLiftEstimate;

        if (exerciseOneRepMaxes.TryGetValue(exerciseName, out var exerciseEstimate))
            return exerciseEstimate;

        // Same exercise logged under a slightly different name (e.g. "Face Pull" vs "Face Pulls", "DB" vs "Dumbbell").
        var match = ExerciseObservations.MatchName(exerciseOneRepMaxes.Keys, exerciseName);
        return match is null ? null : exerciseOneRepMaxes[match];
    }

    public IReadOnlyDictionary<LiftType, double> MainLiftOneRepMaxes => mainLiftOneRepMaxes;
}

public sealed class EstimatedStrengthService(AppDbContext db, IWeightCalculator weightCalculator)
    : IEstimatedStrengthService
{
    /// <summary>
    /// Builds estimates from the user's previous training: for each lift/exercise, the best e1RM within the most
    /// recently trained 5/3/1 cycle or PPL program that contains completed sets for it (not the all-time best).
    /// </summary>
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
                set.Workout.Week.CycleId,
                set.Workout.MainLiftType,
                set.Workout.OccurredOn,
                Weight = set.ActualWeight!.Value,
                Reps = set.ActualReps!.Value
            })
            .ToListAsync();

        // Everything except 5/3/1 main-lift sets (handled above): PPL, 5/3/1 accessories, and additional sessions.
        var exerciseSets = (await ExerciseObservations.LoadAsync(db, userId, weightCalculator))
            .Where(o => o.Kind != ExerciseObservationKind.FiveThreeOneLift)
            .ToList();

        var observations = mainLiftSets
            .Select(set => new Observation($"531:{set.CycleId}", set.MainLiftType, null, set.OccurredOn,
                weightCalculator.CalculateEstimated1RM(set.Weight, set.Reps)))
            .Concat(exerciseSets.Select(set => new Observation(set.ProgramKey,
                set.Kind == ExerciseObservationKind.Ppl ? EstimatedStrengthProfile.MainLiftNameType(set.Name) : null,
                set.Name, set.Date, set.Estimated1Rm)))
            .Where(o => o.Estimate > 0)
            .ToList();
        var sourceRecency = observations
            .GroupBy(o => o.Source)
            .ToDictionary(g => g.Key, g => g.Max(o => o.OccurredOn));

        var mainLiftEstimates = observations
            .Where(o => o.LiftType.HasValue)
            .GroupBy(o => o.LiftType!.Value)
            .ToDictionary(g => g.Key, g => FromMostRecentSource(g, sourceRecency));

        var exerciseEstimates = observations
            .Where(o => o.ExerciseName is not null)
            .GroupBy(o => o.ExerciseName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => FromMostRecentSource(g, sourceRecency), StringComparer.OrdinalIgnoreCase);

        return new EstimatedStrengthProfile(mainLiftEstimates, exerciseEstimates);
    }

    private static double FromMostRecentSource(IEnumerable<Observation> observations, IReadOnlyDictionary<string, DateTime> sourceRecency) =>
        observations
            .GroupBy(o => o.Source)
            .OrderByDescending(g => sourceRecency[g.Key])
            .ThenByDescending(g => g.Key, StringComparer.Ordinal)
            .First()
            .Max(o => o.Estimate);

    private sealed record Observation(string Source, LiftType? LiftType, string? ExerciseName, DateTime OccurredOn, double Estimate);
}
