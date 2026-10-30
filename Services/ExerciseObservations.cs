using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public enum ExerciseObservationKind
{
    FiveThreeOneLift,
    FiveThreeOneAccessory,
    Ppl,
    Additional
}

/// <summary>One completed working set from any program, normalized so every feature reads history the same way.</summary>
public sealed record ExerciseObservation(
    string Name,
    DateTime Date,
    double Weight,
    int Reps,
    double Estimated1Rm,
    ExerciseObservationKind Kind,
    string? SetLabel,
    int Order,
    string SessionKey,
    string ProgramKey)
{
    public string Source => Kind switch
    {
        ExerciseObservationKind.Ppl => "PPL",
        ExerciseObservationKind.Additional => "Additional",
        _ => "5/3/1"
    };
}

/// <summary>The most recent session of an exercise, regardless of which program it was logged in.</summary>
public sealed record ExercisePerformance(string Name, DateTime Date, string Source, IReadOnlyList<ExerciseObservation> Sets)
{
    public ExerciseObservation TopSet => Sets.OrderByDescending(s => s.Weight).ThenByDescending(s => s.Reps).First();
    public double BestEstimated1Rm => Sets.Max(s => s.Estimated1Rm);
}

/// <summary>
/// Shared, user-scoped exercise history across 5/3/1 lifts and accessories, PPL, and additional sessions.
/// Exercise history, starting-weight estimates, "last time" and accessory suggestions all read from here.
/// </summary>
public static class ExerciseObservations
{
    public static async Task<List<ExerciseObservation>> LoadAsync(AppDbContext db, string userId, IWeightCalculator weightCalculator)
    {
        var programmed = await db.WorkoutSets.AsNoTracking()
            .Where(s => s.Workout.Week.Cycle.UserId == userId
                && s.IsCompleted
                && s.SetType != SetType.Warmup
                && s.ActualWeight.HasValue && s.ActualReps.HasValue
                && s.ActualReps!.Value > 0)
            .Select(s => new
            {
                Name = s.Lift.Name,
                Date = s.Workout.OccurredOn,
                Weight = s.ActualWeight!.Value,
                Reps = s.ActualReps!.Value,
                s.SetType,
                s.IsAdditional,
                s.AdditionalSetType,
                Order = s.SetNumber,
                s.WorkoutId,
                s.Workout.Week.CycleId
            })
            .ToListAsync();

        var accessories = await db.WorkoutAccessories.AsNoTracking()
            .Where(x => x.Workout.Week.Cycle.UserId == userId
                && x.IsCompleted && x.Weight > 0 && x.Reps > 0
                && x.Accessory.Category != AccessoryCategory.Cardio)
            .Select(x => new
            {
                Name = x.Accessory.ExerciseDefinition != null ? x.Accessory.ExerciseDefinition.Name : x.Accessory.Name,
                Date = x.Workout.OccurredOn,
                x.Weight,
                x.Reps,
                x.Sets,
                x.WorkoutId,
                x.Workout.Week.CycleId
            })
            .ToListAsync();

        var ppl = await db.PplSessionSets.AsNoTracking()
            .Where(x => x.SessionExercise.Session.Program.UserId == userId
                && x.IsCompleted
                && x.ActualWeight.HasValue && x.ActualReps.HasValue
                && x.ActualReps!.Value > 0)
            .Select(x => new
            {
                Name = x.SessionExercise.ExerciseName,
                Date = x.SessionExercise.Session.OccurredOn,
                Weight = x.ActualWeight!.Value,
                Reps = x.ActualReps!.Value,
                Order = x.SetNumber,
                x.SessionExercise.PplSessionId,
                x.SessionExercise.Session.PplProgramId
            })
            .ToListAsync();

        var additional = await db.AdditionalStrengthSets.AsNoTracking()
            .Where(x => x.Weight.HasValue && x.Reps.HasValue && x.Reps!.Value > 0
                && ((x.Exercise.Session.WeekId != null && x.Exercise.Session.Week!.Cycle.UserId == userId)
                    || (x.Exercise.Session.CycleId != null && x.Exercise.Session.Cycle!.UserId == userId)
                    || (x.Exercise.Session.PplWeekId != null && x.Exercise.Session.PplWeek!.Program.UserId == userId)))
            .Select(x => new
            {
                Name = x.Exercise.ExerciseName,
                Date = x.Exercise.Session.OccurredOn,
                Weight = x.Weight!.Value,
                Reps = x.Reps!.Value,
                Order = x.SetNumber,
                x.Exercise.AdditionalSessionId,
                WeekCycleId = x.Exercise.Session.WeekId != null ? (int?)x.Exercise.Session.Week!.CycleId : null,
                x.Exercise.Session.CycleId,
                PplProgramId = x.Exercise.Session.PplWeekId != null ? (int?)x.Exercise.Session.PplWeek!.PplProgramId : null
            })
            .ToListAsync();

        var results = new List<ExerciseObservation>(programmed.Count + accessories.Count + ppl.Count + additional.Count);

        results.AddRange(programmed
            .Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Weight > 0)
            .Select(x => new ExerciseObservation(
                x.Name.Trim(), x.Date, x.Weight, x.Reps,
                weightCalculator.CalculateEstimated1RM(x.Weight, x.Reps),
                ExerciseObservationKind.FiveThreeOneLift,
                x.IsAdditional ? x.AdditionalSetType.ToString() : DescribeSetType(x.SetType),
                x.Order, $"531:w{x.WorkoutId}", $"531:{x.CycleId}")));

        results.AddRange(accessories
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .SelectMany(x => Enumerable.Range(1, Math.Max(1, x.Sets)).Select(setNumber => new ExerciseObservation(
                x.Name.Trim(), x.Date, x.Weight, x.Reps,
                weightCalculator.CalculateEstimated1RM(x.Weight, x.Reps),
                ExerciseObservationKind.FiveThreeOneAccessory,
                "Accessory", setNumber, $"531:w{x.WorkoutId}", $"531:{x.CycleId}"))));

        results.AddRange(ppl
            .Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Weight > 0)
            .Select(x => new ExerciseObservation(
                x.Name.Trim(), x.Date, x.Weight, x.Reps,
                weightCalculator.CalculateEstimated1RM(x.Weight, x.Reps),
                ExerciseObservationKind.Ppl, null, x.Order,
                $"ppl:s{x.PplSessionId}", $"ppl:{x.PplProgramId}")));

        results.AddRange(additional
            .Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Weight > 0)
            .Select(x => new ExerciseObservation(
                x.Name.Trim(), x.Date, x.Weight, x.Reps,
                weightCalculator.CalculateEstimated1RM(x.Weight, x.Reps),
                ExerciseObservationKind.Additional, null, x.Order,
                $"add:s{x.AdditionalSessionId}",
                (x.WeekCycleId ?? x.CycleId) is int cycleId ? $"531:{cycleId}"
                    : x.PplProgramId is int programId ? $"ppl:{programId}"
                    : $"add:s{x.AdditionalSessionId}")));

        return results;
    }

    /// <summary>
    /// Finds the logged name that represents <paramref name="exerciseName"/>: an exact (case/punctuation-insensitive)
    /// match first, then the closest name that <see cref="ExerciseNameMatcher"/> would auto-match.
    /// </summary>
    public static string? MatchName(IEnumerable<string> loggedNames, string exerciseName)
    {
        var key = ExerciseHistoryService.BuildKey(exerciseName);
        var names = loggedNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var exact = names.FirstOrDefault(n => ExerciseHistoryService.BuildKey(n) == key);
        if (exact is not null) return exact;

        var best = names
            .Select(n => (Name: n, Score: ExerciseNameMatcher.Score(exerciseName, n)))
            .OrderByDescending(m => m.Score)
            .FirstOrDefault();
        return best.Name is not null && best.Score >= ExerciseNameMatcher.AutoMatchThreshold ? best.Name : null;
    }

    /// <summary>Latest session of the matching exercise, optionally limited to observations passing <paramref name="filter"/>.</summary>
    public static ExercisePerformance? Latest(
        IEnumerable<ExerciseObservation> observations,
        string exerciseName,
        Func<ExerciseObservation, bool>? filter = null)
    {
        var candidates = (filter is null ? observations : observations.Where(filter)).ToList();
        var name = MatchName(candidates.Select(o => o.Name), exerciseName);
        if (name is null) return null;

        var sets = candidates.Where(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        var latest = sets.OrderByDescending(o => o.Date).ThenByDescending(o => o.SessionKey, StringComparer.Ordinal).First();
        var sessionSets = sets.Where(o => o.SessionKey == latest.SessionKey && o.Date.Date == latest.Date.Date)
            .OrderBy(o => o.Order)
            .ToList();
        return new ExercisePerformance(name, latest.Date, latest.Source, sessionSets);
    }

    private static string DescribeSetType(SetType setType) => setType switch
    {
        SetType.Bbb => "BBB",
        SetType.Fsl => "FSL",
        _ => setType.ToString()
    };
}
