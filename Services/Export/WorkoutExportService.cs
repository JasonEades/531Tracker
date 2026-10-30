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
                WorkoutExportScope.PplSession => await BuildPplSessionDocumentAsync(request.Id, cancellationToken),
                WorkoutExportScope.PplWeek => await BuildPplWeekDocumentAsync(request.Id, cancellationToken),
                WorkoutExportScope.PplProgram => await BuildPplProgramDocumentAsync(request.Id, cancellationToken),
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

        var workoutDate = workout.OccurredOn.Date;
        var dailySteps = await GetDailyStepsAsync(userId, workoutDate, workoutDate.AddDays(1), cancellationToken);
        return new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Day,
            Workout = MapWorkout(workout, dailySteps)
        };
    }

    private async Task<WorkoutExportDocument> BuildWeekDocumentAsync(int weekId, CancellationToken cancellationToken)
    {
        var userId = await userContext.GetUserIdAsync();
        var week = await WeekQuery()
            .Where(w => w.Id == weekId && w.Cycle.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new WorkoutExportException("The training week could not be found.");

        var cycleStart = (week.Cycle.StartDate ?? week.Cycle.CreatedAt).Date;
        var weekStart = cycleStart.AddDays(((int)week.WeekNumber - 1) * 7);
        var dailySteps = await GetDailyStepsAsync(userId, weekStart, weekStart.AddDays(7), cancellationToken);
        return new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Week,
            Week = MapWeek(week, dailySteps)
        };
    }

    private async Task<WorkoutExportDocument> BuildCycleDocumentAsync(int cycleId, CancellationToken cancellationToken)
    {
        var userId = await userContext.GetUserIdAsync();
        var cycle = await CycleQuery()
            .Where(c => c.Id == cycleId && c.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new WorkoutExportException("The training cycle could not be found.");

        var cycleStart = (cycle.StartDate ?? cycle.CreatedAt).Date;
        var dailySteps = await GetDailyStepsAsync(userId, cycleStart, cycleStart.AddDays(28), cancellationToken);
        return new WorkoutExportDocument
        {
            Scope = WorkoutExportScope.Cycle,
            Cycle = MapCycle(cycle, dailySteps)
        };
    }

    private const string PplProgramName = "Push/Pull/Legs";

    private async Task<WorkoutExportDocument> BuildPplSessionDocumentAsync(int sessionId, CancellationToken cancellationToken)
    {
        var userId = await userContext.GetUserIdAsync();
        var session = await PplSessionQuery()
            .Include(s => s.Program)
            .Include(s => s.Week)
            .Where(s => s.Id == sessionId && s.Program.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new WorkoutExportException("The PPL session could not be found.");

        var sessionDate = session.OccurredOn.Date;
        var dailySteps = await GetDailyStepsAsync(userId, sessionDate, sessionDate.AddDays(1), cancellationToken);
        var workout = MapPplSession(session, session.Program.Name, session.Week?.WeekNumber ?? 0, dailySteps);
        return new WorkoutExportDocument
        {
            ProgramName = PplProgramName,
            Scope = WorkoutExportScope.Day,
            Workout = workout,
            FileStem = $"PPL_Workout_{sessionDate:yyyy-MM-dd}_{workout.WorkoutName}"
        };
    }

    private async Task<WorkoutExportDocument> BuildPplWeekDocumentAsync(int pplWeekId, CancellationToken cancellationToken)
    {
        var userId = await userContext.GetUserIdAsync();
        var week = await PplWeekQuery()
            .Include(w => w.Program)
            .Where(w => w.Id == pplWeekId && w.Program.UserId == userId)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new WorkoutExportException("The PPL training week could not be found.");

        var weekStart = week.StartDate.Date;
        var dailySteps = await GetDailyStepsAsync(userId, weekStart, weekStart.AddDays(7), cancellationToken);
        return new WorkoutExportDocument
        {
            ProgramName = PplProgramName,
            Scope = WorkoutExportScope.Week,
            Week = MapPplWeek(week, week.Program.Name, dailySteps),
            FileStem = $"PPL_Week_{week.WeekNumber:00}_{week.Program.Name}"
        };
    }

    private async Task<WorkoutExportDocument> BuildPplProgramDocumentAsync(int programId, CancellationToken cancellationToken)
    {
        var userId = await userContext.GetUserIdAsync();
        var program = await db.PplPrograms.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == programId && p.UserId == userId, cancellationToken)
            ?? throw new WorkoutExportException("The PPL program could not be found.");
        var weeks = await PplWeekQuery()
            .Where(w => w.PplProgramId == programId)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync(cancellationToken);

        var dailySteps = weeks.Count == 0
            ? []
            : await GetDailyStepsAsync(userId, weeks.Min(w => w.StartDate).Date, weeks.Max(w => w.StartDate).Date.AddDays(7), cancellationToken);
        var weekModels = weeks.Select(w => MapPplWeek(w, program.Name)).ToList();
        return new WorkoutExportDocument
        {
            ProgramName = PplProgramName,
            Scope = WorkoutExportScope.Cycle,
            Cycle = new CycleExportModel
            {
                Id = program.Id,
                Name = program.Name,
                Noun = "Program",
                ContextValue = $"{program.DaysPerWeek}-day Push/Pull/Legs split",
                StatusLabel = program.IsActive ? "Active" : "Inactive",
                CreatedAt = program.CreatedAt,
                Notes = program.Notes,
                DailySteps = dailySteps,
                Weeks = weekModels,
                Summary = Summarize(weekModels)
            },
            FileStem = $"PPL_Program_{program.Name}"
        };
    }

    private IQueryable<PplSession> PplSessionQuery() => db.PplSessions
        .AsNoTracking()
        .Include(s => s.DayTemplate)
        .Include(s => s.Exercises).ThenInclude(e => e.ExerciseSlot)
        .Include(s => s.Exercises).ThenInclude(e => e.Sets)
        .AsSplitQuery();

    private IQueryable<PplWeek> PplWeekQuery() => db.PplWeeks
        .AsNoTracking()
        .Include(w => w.Sessions).ThenInclude(s => s.DayTemplate)
        .Include(w => w.Sessions).ThenInclude(s => s.Exercises).ThenInclude(e => e.ExerciseSlot)
        .Include(w => w.Sessions).ThenInclude(s => s.Exercises).ThenInclude(e => e.Sets)
        .Include(w => w.AdditionalSessions).ThenInclude(s => s.StrengthExercises).ThenInclude(e => e.Sets)
        .Include(w => w.AdditionalSessions).ThenInclude(s => s.CardioEntries).ThenInclude(e => e.Accessory)
        .AsSplitQuery();

    private static WeekExportModel MapPplWeek(PplWeek week, string programName, List<DailyStepExportModel>? dailySteps = null)
    {
        var workouts = week.Sessions
            .OrderBy(s => s.OccurredOn).ThenBy(s => s.CreatedAt)
            .Select(s => MapPplSession(s, programName, week.WeekNumber))
            .ToList();
        var additional = week.AdditionalSessions
            .Where(s => !IsImportedStepSession(s))
            .OrderBy(s => s.OccurredOn)
            .ThenBy(s => s.CreatedAt)
            .Select(MapAdditionalSession)
            .ToList();
        return new WeekExportModel
        {
            Id = week.Id,
            WeekNumber = week.WeekNumber,
            ContextValue = programName,
            Workouts = workouts,
            AdditionalSessions = additional,
            DailySteps = dailySteps ?? [],
            Summary = Summarize(workouts)
        };
    }

    private static WorkoutExportModel MapPplSession(
        PplSession session, string programName, int weekNumber, List<DailyStepExportModel>? dailySteps = null)
    {
        var exercises = session.Exercises.OrderBy(e => e.OrderInSession).Select(e => new ExerciseExportModel
        {
            Name = e.ExerciseName,
            Category = e.ExerciseSlot?.MuscleGroup.ToString() ?? "Working",
            Sets = e.Sets.OrderBy(s => s.SetNumber).Select(s => new SetExportModel
            {
                Number = s.SetNumber,
                Type = e.RepsMin == e.RepsMax ? $"{e.RepsMax} reps" : $"{e.RepsMin}–{e.RepsMax} reps",
                TargetReps = s.TargetReps,
                TargetWeight = e.SuggestedWeight ?? 0,
                ActualReps = s.ActualReps,
                ActualWeight = s.ActualWeight,
                IsCompleted = s.IsCompleted
            }).ToList()
        }).ToList();

        return new WorkoutExportModel
        {
            Id = session.Id,
            WeekNumber = weekNumber,
            ProgramName = PplProgramName,
            ContextLabel = "Plan",
            ContextValue = programName,
            WorkoutName = session.DayTemplate.Name,
            WorkoutType = session.DayTemplate.DayType.ToString(),
            WorkoutDate = session.OccurredOn,
            Status = session.Status.ToString(),
            Notes = session.Notes,
            Exercises = exercises,
            DailySteps = dailySteps ?? [],
            Summary = Summarize(exercises, [])
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


    private static CycleExportModel MapCycle(Cycle cycle, List<DailyStepExportModel> dailySteps)
    {
        var weeks = cycle.Weeks.OrderBy(w => w.WeekNumber).Select(w => MapWeek(w)).ToList();
        var additional = cycle.AdditionalSessions
            .Where(s => !IsImportedStepSession(s))
            .OrderBy(s => s.OccurredOn)
            .ThenBy(s => s.CreatedAt)
            .Select(MapAdditionalSession)
            .ToList();
        return new CycleExportModel
        {
            Id = cycle.Id,
            CycleNumber = cycle.CycleNumber,
            Name = cycle.Name,
            CreatedAt = cycle.CreatedAt,
            IsCompleted = cycle.IsCompleted,
            Notes = cycle.Notes,
            AdditionalSessions = additional,
            DailySteps = dailySteps,
            Weeks = weeks,
            Summary = Summarize(weeks)
        };
    }

    private static WeekExportModel MapWeek(Week week, List<DailyStepExportModel>? dailySteps = null)
    {
        var workouts = week.Workouts.OrderBy(w => w.OccurredOn).ThenBy(w => w.CreatedAt).ThenBy(w => w.MainLiftType).Select(w => MapWorkout(w)).ToList();
        var additional = week.AdditionalSessions
            .Where(s => !IsImportedStepSession(s))
            .OrderBy(s => s.OccurredOn)
            .ThenBy(s => s.CreatedAt)
            .Select(MapAdditionalSession)
            .ToList();
        return new WeekExportModel
        {
            Id = week.Id,
            WeekNumber = (int)week.WeekNumber,
            CycleNumber = week.Cycle.CycleNumber,
            Notes = week.Notes,
            Workouts = workouts,
            AdditionalSessions = additional,
            DailySteps = dailySteps ?? [],
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

    private static bool IsImportedStepSession(AdditionalSession session) =>
        session.SessionType == SessionType.Cardio &&
        session.Name == "Google Health — Steps";

    private static WorkoutExportModel MapWorkout(Workout workout, List<DailyStepExportModel>? dailySteps = null)
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
            DailySteps = dailySteps ?? [],
            Summary = Summarize(exercises, accessories)
        };
    }

    private async Task<List<DailyStepExportModel>> GetDailyStepsAsync(
        string userId, DateTime? startInclusive, DateTime? endExclusive, CancellationToken cancellationToken)
    {
        if (startInclusive is null || endExclusive is null)
            return [];

        return await db.DailyStepRecords.AsNoTracking()
            .Where(x => x.UserId == userId && x.LocalDate >= startInclusive.Value && x.LocalDate < endExclusive.Value)
            .OrderBy(x => x.LocalDate)
            .Select(x => new DailyStepExportModel { Date = x.LocalDate, Steps = x.StepCount, Provider = x.Provider })
            .ToListAsync(cancellationToken);
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
        var stem = document.FileStem ?? document.Scope switch
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
