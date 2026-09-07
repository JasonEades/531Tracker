using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services.Export;

public interface IWorkoutExportService
{
    Task<WorkoutExportResult> ExportAsync(WorkoutExportRequest request, CancellationToken cancellationToken = default);
}

public sealed class WorkoutExportException(string message, Exception? innerException = null) : Exception(message, innerException);

public sealed class WorkoutExportService(
    AppDbContext db,
    ICurrentUserService userContext,
    IEnumerable<IWorkoutExportRenderer> renderers,
    ILogger<WorkoutExportService> logger) : IWorkoutExportService
{
    public async Task<WorkoutExportResult> ExportAsync(WorkoutExportRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = request.Scope switch
            {
                WorkoutExportScope.Day => await BuildDayDocumentAsync(request.Id, cancellationToken),
                WorkoutExportScope.Week => await BuildWeekDocumentAsync(request.Id, cancellationToken),
                WorkoutExportScope.Cycle => await BuildCycleDocumentAsync(request.Id, cancellationToken),
                _ => throw new WorkoutExportException("The requested export scope is not supported.")
            };

            var renderer = renderers.FirstOrDefault(r => r.Format == request.Format)
                ?? throw new WorkoutExportException("The requested export format is not available.");
            var content = renderer.Render(document);

            return new WorkoutExportResult
            {
                Content = content,
                ContentType = request.Format == WorkoutExportFormat.Pdf ? "application/pdf" : "text/markdown; charset=utf-8",
                FileName = BuildFileName(document, request.Format)
            };
        }
        catch (WorkoutExportException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export {Scope} with ID {Id} as {Format}.", request.Scope, request.Id, request.Format);
            throw new WorkoutExportException("The export could not be generated. Please try again.", ex);
        }
    }

    private async Task<WorkoutExportDocument> BuildDayDocumentAsync(int workoutId, CancellationToken cancellationToken)
    {
        var userId = await userContext.GetUserIdAsync();
        var workout = await WorkoutQuery()
            .Where(w => w.Id == workoutId && w.Week.Cycle.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new WorkoutExportException("The workout could not be found.");

        return new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Day,
            Workout = MapWorkout(workout)
        };
    }

    private async Task<WorkoutExportDocument> BuildWeekDocumentAsync(int weekId, CancellationToken cancellationToken)
    {
        var userId = await userContext.GetUserIdAsync();
        var week = await WeekQuery()
            .Where(w => w.Id == weekId && w.Cycle.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new WorkoutExportException("The training week could not be found.");

        return new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Week,
            Week = MapWeek(week)
        };
    }

    private async Task<WorkoutExportDocument> BuildCycleDocumentAsync(int cycleId, CancellationToken cancellationToken)
    {
        var userId = await userContext.GetUserIdAsync();
        var cycle = await CycleQuery()
            .Where(c => c.Id == cycleId && c.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new WorkoutExportException("The training cycle could not be found.");

        return new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Cycle,
            Cycle = MapCycle(cycle)
        };
    }

    private IQueryable<Workout> WorkoutQuery() => db.Workouts
        .AsNoTracking()
        .Include(w => w.Week).ThenInclude(w => w.Cycle)
        .Include(w => w.Sets).ThenInclude(s => s.Lift)
        .Include(w => w.WorkoutAccessories).ThenInclude(wa => wa.Accessory);

    private IQueryable<Week> WeekQuery() => db.Weeks
        .AsNoTracking()
        .Include(w => w.Cycle)
        .Include(w => w.Workouts).ThenInclude(wo => wo.Sets).ThenInclude(s => s.Lift)
        .Include(w => w.Workouts).ThenInclude(wo => wo.WorkoutAccessories).ThenInclude(wa => wa.Accessory)
        .Include(w => w.AdditionalSessions).ThenInclude(s => s.StrengthExercises).ThenInclude(e => e.Sets)
        .Include(w => w.AdditionalSessions).ThenInclude(s => s.CardioEntries).ThenInclude(e => e.Accessory);


    private IQueryable<Cycle> CycleQuery() => db.Cycles
        .AsNoTracking()
        .Include(c => c.Weeks).ThenInclude(w => w.Workouts).ThenInclude(wo => wo.Sets).ThenInclude(s => s.Lift)
        .Include(c => c.Weeks).ThenInclude(w => w.Workouts).ThenInclude(wo => wo.WorkoutAccessories).ThenInclude(wa => wa.Accessory)
        .Include(c => c.Weeks).ThenInclude(w => w.AdditionalSessions).ThenInclude(s => s.StrengthExercises).ThenInclude(e => e.Sets)
        .Include(c => c.Weeks).ThenInclude(w => w.AdditionalSessions).ThenInclude(s => s.CardioEntries).ThenInclude(e => e.Accessory)
        .Include(c => c.AdditionalSessions).ThenInclude(s => s.StrengthExercises).ThenInclude(e => e.Sets)
        .Include(c => c.AdditionalSessions).ThenInclude(s => s.CardioEntries).ThenInclude(e => e.Accessory);


    private static CycleExportModel MapCycle(Cycle cycle)
    {
        var weeks = cycle.Weeks.OrderBy(w => w.WeekNumber).Select(MapWeek).ToList();
        var additional = cycle.AdditionalSessions.OrderBy(s => s.OccurredOn).ThenBy(s => s.CreatedAt).Select(MapAdditionalSession).ToList();
        return new CycleExportModel
        {
            Id = cycle.Id,
            CycleNumber = cycle.CycleNumber,
            Name = cycle.Name,
            CreatedAt = cycle.CreatedAt,
            IsCompleted = cycle.IsCompleted,
            Notes = cycle.Notes,
            AdditionalSessions = additional,
            Weeks = weeks,
            Summary = Summarize(weeks)
        };
    }

    private static WeekExportModel MapWeek(Week week)
    {
        var workouts = week.Workouts.OrderBy(w => w.OccurredOn).ThenBy(w => w.CreatedAt).ThenBy(w => w.MainLiftType).Select(MapWorkout).ToList();
        var additional = week.AdditionalSessions.OrderBy(s => s.OccurredOn).ThenBy(s => s.CreatedAt).Select(MapAdditionalSession).ToList();
        return new WeekExportModel
        {
            Id = week.Id,
            WeekNumber = (int)week.WeekNumber,
            CycleNumber = week.Cycle.CycleNumber,
            Notes = week.Notes,
            Workouts = workouts,
            AdditionalSessions = additional,
            Summary = Summarize(workouts)
        };
    }

    private static AdditionalSessionExportModel MapAdditionalSession(AdditionalSession session) => new()
    {
        Date = session.OccurredOn,
        SessionType = session.SessionType.ToString(),
        Name = session.Name,
        Notes = session.Notes,
        Exercises = session.StrengthExercises.OrderBy(e => e.Order).Select(e => new AdditionalExerciseExportModel
        {
            Name = e.ExerciseName,
            Sets = e.Sets.OrderBy(s => s.SetNumber).Select(s => new AdditionalSetExportModel { Number = s.SetNumber, Weight = s.Weight, Reps = s.Reps, Notes = s.Notes }).ToList()
        }).ToList(),
        CardioEntries = session.CardioEntries.OrderBy(e => e.Id).Select(e => new CardioExportModel { Exercise = e.Accessory.Name, Quantity = e.Quantity, Unit = e.Unit.ToString(), Notes = e.Notes, Source = e.Source }).ToList()
    };

    private static WorkoutExportModel MapWorkout(Workout workout)
    {
        var exercises = workout.Sets
            .GroupBy(s => s.Lift)
            .OrderBy(g => g.Key.LiftType)
            .Select(group => MapExercise(group, workout)).ToList();

        var accessories = workout.WorkoutAccessories.OrderBy(wa => wa.Accessory.Name).Select(wa => new AccessoryExportModel
        {
            Name = wa.Accessory.Name,
            Description = wa.Accessory.Description,
            Sets = wa.Sets,
            Reps = wa.Reps,
            Weight = wa.Weight,
            Notes = wa.Notes,
            IsCompleted = wa.IsCompleted
        }).ToList();

        return new WorkoutExportModel
        {
            Id = workout.Id,
            CycleNumber = workout.Week.Cycle.CycleNumber,
            WeekNumber = (int)workout.Week.WeekNumber,
            WorkoutName = $"{DisplayName(workout.MainLiftType)} Day",
            WorkoutType = workout.MainLiftType.ToString(),
            WorkoutDate = workout.OccurredOn,
            Status = workout.Status.ToString(),
            Notes = workout.Notes,
            Exercises = exercises,
            Accessories = accessories,
            Summary = Summarize(exercises, accessories)
        };
    }

    private static ExerciseExportModel MapExercise(IGrouping<Lift, WorkoutSet> group, Workout workout)
    {
        var programmed = group.Where(s => !s.IsAdditional).ToList();
        var mainSetNumbers = programmed.Where(s => s.SetType == SetType.Main).Select(s => s.SetNumber).ToHashSet();
        return new ExerciseExportModel
        {
            Name = group.Key.Name,
            Category = string.Join(", ", programmed.Select(s => s.SetType.ToString()).Distinct()),
            Sets = programmed.OrderBy(s => s.SetType).ThenBy(s => s.SetNumber).Select(s => MapSet(s, s.SetType.ToString(),
                s.SetType == SetType.Main && mainSetNumbers.Count > 0 && workout.Week.WeekNumber == WeekNumber.Week3 && s.SetNumber == mainSetNumbers.Max())).ToList(),
            AdditionalSets = group.Where(s => s.IsAdditional).OrderBy(s => s.Sequence).Select(s => MapSet(s, s.AdditionalSetType.ToString(), false)).ToList()
        };
    }

    private static SetExportModel MapSet(WorkoutSet set, string type, bool isAmrap) => new()
    {
        Number = set.IsAdditional ? set.Sequence : set.SetNumber,
        Type = type,
        TargetReps = set.PrescribedReps,
        TargetWeight = set.PrescribedWeight,
        ActualReps = set.ActualReps,
        ActualWeight = set.ActualWeight,
        IsCompleted = set.IsCompleted,
        Notes = set.Notes,
        IsAmrap = isAmrap,
        AdditionalType = set.IsAdditional ? set.AdditionalSetType.ToString() : null,
        Rpe = set.Rpe,
        Rir = set.Rir
    };

    private static ExportSummaryModel Summarize(IEnumerable<WeekExportModel> weeks) => Summarize(weeks.SelectMany(w => w.Workouts));

    private static ExportSummaryModel Summarize(IEnumerable<WorkoutExportModel> workouts)
    {
        var items = workouts.ToList();
        return new ExportSummaryModel
        {
            WorkoutCount = items.Count,
            CompletedWorkoutCount = items.Count(w => w.Status == nameof(WorkoutStatus.Completed)),
            ExerciseCount = items.Sum(w => w.Summary.ExerciseCount),
            SetCount = items.Sum(w => w.Summary.SetCount),
            CompletedSetCount = items.Sum(w => w.Summary.CompletedSetCount),
            TotalReps = items.Sum(w => w.Summary.TotalReps),
            TotalVolume = items.Sum(w => w.Summary.TotalVolume)
        };
    }

    private static ExportSummaryModel Summarize(IEnumerable<ExerciseExportModel> exercises, IEnumerable<AccessoryExportModel> accessories)
    {
        var exerciseList = exercises.ToList();
        var accessoryList = accessories.ToList();
        return new ExportSummaryModel
        {
            ExerciseCount = exerciseList.Count + accessoryList.Count,
            SetCount = exerciseList.Sum(e => e.Sets.Count + e.AdditionalSets.Count) + accessoryList.Sum(a => a.Sets),
            CompletedSetCount = exerciseList.Sum(e => e.Sets.Count(s => s.IsCompleted) + e.AdditionalSets.Count(s => s.IsCompleted)) + accessoryList.Where(a => a.IsCompleted).Sum(a => a.Sets),
            TotalReps = exerciseList.Sum(e => e.Sets.Concat(e.AdditionalSets).Sum(s => s.ActualReps ?? s.TargetReps)) + accessoryList.Sum(a => a.Sets * a.Reps),
            TotalVolume = exerciseList.Sum(e => e.Sets.Concat(e.AdditionalSets).Sum(s => (s.ActualWeight ?? s.TargetWeight) * (s.ActualReps ?? s.TargetReps))) + accessoryList.Sum(a => a.Weight * a.Reps * a.Sets)
        };
    }

    private static string BuildFileName(WorkoutExportDocument document, WorkoutExportFormat format)
    {
        var stem = document.Scope switch
        {
            WorkoutExportScope.Day => $"Workout_{document.Workout!.WorkoutDate?.ToString("yyyy-MM-dd") ?? "undated"}_{document.Workout.WorkoutName}",
            WorkoutExportScope.Week => $"Week_{document.Week!.WeekNumber:00}_Cycle_{document.Week.CycleNumber}",
            WorkoutExportScope.Cycle => $"Cycle_{document.Cycle!.CycleNumber}",
            _ => "WorkoutExport"
        };
        return $"{SanitizeFileName(stem)}.{(format == WorkoutExportFormat.Pdf ? "pdf" : "md")}";
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(c => invalid.Contains(c) ? '-' : c)).Trim();
    }

    private static string DisplayName(LiftType liftType) => liftType switch
    {
        LiftType.BenchPress => "Bench Press",
        LiftType.OverheadPress => "Overhead Press",
        _ => liftType.ToString()
    };
}
