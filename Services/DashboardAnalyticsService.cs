using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IDashboardAnalyticsService
{
    Task<DashboardAnalytics> GetDashboardAsync();
}

public sealed class DashboardAnalyticsService(
    AppDbContext db,
    ICurrentUserService userContext,
    IWeightCalculator weightCalculator) : IDashboardAnalyticsService
{
    public async Task<DashboardAnalytics> GetDashboardAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        var today = DateTime.UtcNow.Date;
        var yearStart = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = StartOfWeek(today);
        var historyStart = today.AddYears(-1);
        var hasGoogleHealth = await db.GoogleHealthConnections.AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.Status == HealthConnectionStatus.Connected);

        var currentCycle = await db.Cycles
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsCompleted)
            .OrderByDescending(c => c.CycleNumber)
            .Select(c => new CurrentCycleRow
            {
                Id = c.Id,
                Name = c.Name,
                CurrentWeek = c.Weeks
                    .Where(w => w.Workouts.Any(x => x.Status != WorkoutStatus.Completed))
                    .OrderBy(w => w.WeekNumber)
                    .Select(w => "Week " + (int)w.WeekNumber)
                    .FirstOrDefault(),
                Planned = c.Weeks.SelectMany(w => w.Workouts).Count(),
                Completed = c.Weeks.SelectMany(w => w.Workouts).Count(w => w.Status == WorkoutStatus.Completed),
                Additional = c.AdditionalSessions.Count(s => s.Status == WorkoutStatus.Completed)
            })
            .FirstOrDefaultAsync();

        var workouts = await db.Workouts.AsNoTracking()
            .Where(w => w.Week.Cycle.UserId == userId && w.Status == WorkoutStatus.Completed && w.OccurredOn >= historyStart)
            .Select(w => new WorkoutRow
            {
                Date = w.OccurredOn,
                CycleId = w.Week.CycleId,
                CycleNumber = w.Week.Cycle.CycleNumber,
                LiftType = w.MainLiftType,
                Sets = w.Sets.Where(s => s.IsCompleted && s.ActualWeight.HasValue && s.ActualReps.HasValue)
                    .Select(s => new ActualSetRow { Weight = s.ActualWeight!.Value, Reps = s.ActualReps!.Value, LiftName = s.Lift.Name })
                    .ToList()
            })
            .ToListAsync();

        var stepRecords = await db.DailyStepRecords.AsNoTracking()
            .Where(x => x.UserId == userId && x.LocalDate >= historyStart)
            .Select(x => new DailyStepPoint { Date = x.LocalDate, Steps = x.StepCount })
            .ToListAsync();

        var pplSessions = await db.PplSessions.AsNoTracking()
            .Where(s => s.Program.UserId == userId && s.Status == WorkoutStatus.Completed && s.OccurredOn >= historyStart)
            .Select(s => new PplSessionRow
            {
                Date = s.OccurredOn,
                Exercises = s.Exercises.Select(e => new PplExerciseRow
                {
                    Key = e.PplExerciseSlotId.ToString(),
                    Name = e.ExerciseName,
                    Sets = e.Sets.Where(x => x.IsCompleted && x.ActualWeight.HasValue && x.ActualReps.HasValue)
                        .Select(x => new ActualSetRow { Weight = x.ActualWeight!.Value, Reps = x.ActualReps!.Value, LiftName = e.ExerciseName })
                        .ToList()
                }).ToList()
            })
            .ToListAsync();

        var additional = await db.AdditionalSessions.AsNoTracking()
            .Where(s => s.Status == WorkoutStatus.Completed &&
                ((s.WeekId != null && s.Week!.Cycle.UserId == userId) ||
                 (s.CycleId != null && s.Cycle!.UserId == userId) ||
                 (s.PplWeekId != null && s.PplWeek!.Program.UserId == userId)) &&
                s.OccurredOn >= historyStart)
            .Select(s => new AdditionalRow
            {
                Date = s.OccurredOn,
                Type = s.SessionType,
                Cardio = s.CardioEntries.Select(e => new CardioRow { Quantity = e.Quantity, Unit = e.Unit }).ToList(),
                StrengthSets = s.StrengthExercises.SelectMany(e => e.Sets
                    .Where(x => x.Weight.HasValue && x.Reps.HasValue)
                    .Select(x => new ActualSetRow { Weight = x.Weight!.Value, Reps = x.Reps!.Value, LiftName = e.ExerciseName }))
                    .ToList()
            })
            .ToListAsync();

        var currentProgram = BuildCurrentProgram(currentCycle, pplSessions, additional, today);
        var allSessions = workouts.Select(w => w.Date).Concat(pplSessions.Select(s => s.Date)).Concat(additional.Select(s => s.Date));
        var yearWorkouts = workouts.Where(w => InYear(w.Date, yearStart)).ToList();
        var yearPpl = pplSessions.Where(s => InYear(s.Date, yearStart)).ToList();
        var yearAdditional = additional.Where(s => InYear(s.Date, yearStart)).ToList();

        return new DashboardAnalytics
        {
            Program = currentProgram,
            ThisWeek = BuildWeek(workouts, pplSessions, additional, weekStart),
            YearToDate = BuildYear(yearStart.Year, yearWorkouts, yearPpl, yearAdditional),
            MonthlyTraining = BuildMonthly(yearStart.Year, yearWorkouts, yearPpl, yearAdditional),
            StrengthProgress = BuildStrengthProgress(workouts, pplSessions, today, currentCycle?.Id),
            RecentRecords = BuildRecords(workouts, pplSessions),
            Steps = BuildSteps(stepRecords, today, weekStart, yearStart, hasGoogleHealth)
        };
    }

    private static StepAnalytics BuildSteps(List<DailyStepPoint> records, DateTime today, DateTime weekStart, DateTime yearStart, bool isConnected)
    {
        var week = records.Where(x => x.Date >= weekStart && x.Date <= today).OrderBy(x => x.Date).ToList();
        var year = records.Where(x => x.Date >= yearStart && x.Date <= today).ToList();
        var last30Start = today.AddDays(-29);
        var recordsByDate = records.ToDictionary(x => x.Date.Date, x => x.Steps);
        var last30 = Enumerable.Range(0, 30)
            .Select(offset =>
            {
                var date = last30Start.AddDays(offset);
                return new DailyStepPoint { Date = date, Steps = recordsByDate.GetValueOrDefault(date) };
            })
            .ToList();
        return new StepAnalytics
        {
            IsConnected = isConnected,
            Today = records.Where(x => x.Date == today).Select(x => x.Steps).FirstOrDefault(),
            ThisWeekTotal = week.Sum(x => x.Steps),
            ThisWeekAverage = week.Count == 0 ? 0 : week.Average(x => x.Steps),
            ThisWeekHighest = week.Count == 0 ? 0 : week.Max(x => x.Steps),
            ThisWeekLowest = week.Count == 0 ? 0 : week.Min(x => x.Steps),
            ThisWeekDaysWithData = week.Count,
            YearTotal = year.Sum(x => x.Steps),
            YearAverage = year.Count == 0 ? 0 : year.Average(x => x.Steps),
            ThisWeek = week,
            Last30Days = last30
        };
    }

    private CurrentProgramAnalytics BuildCurrentProgram(CurrentCycleRow? cycle, List<PplSessionRow> ppl, List<AdditionalRow> additional, DateTime today)
    {
        if (cycle is not null)
            return new CurrentProgramAnalytics { Name = cycle.Name, ProgramType = "5/3/1", CurrentWeek = cycle.CurrentWeek ?? "Complete", PlannedSessions = cycle.Planned, CompletedSessions = cycle.Completed, AdditionalSessions = cycle.Additional };

        var completedPpl = ppl.Count(s => s.Date <= today);
        return new CurrentProgramAnalytics { ProgramType = "PPL", PlannedSessions = completedPpl, CompletedSessions = completedPpl, CurrentWeek = completedPpl == 0 ? null : $"{completedPpl} sessions logged", AdditionalSessions = additional.Count };
    }

    private WeeklyAnalytics BuildWeek(List<WorkoutRow> workouts, List<PplSessionRow> ppl, List<AdditionalRow> additional, DateTime start)
    {
        var w = workouts.Where(x => x.Date >= start).ToList();
        var p = ppl.Where(x => x.Date >= start).ToList();
        var a = additional.Where(x => x.Date >= start).ToList();
        var sets = w.SelectMany(x => x.Sets).Concat(p.SelectMany(x => x.Exercises).SelectMany(x => x.Sets)).Concat(a.SelectMany(x => x.StrengthSets)).ToList();
        return new WeeklyAnalytics
        {
            ProgrammedWorkouts = w.Count,
            PplSessions = p.Count,
            AdditionalSessions = a.Count,
            CardioSessions = a.Count(x => x.Type == SessionType.Cardio),
            CompletedSets = sets.Count,
            TotalReps = sets.Sum(x => x.Reps),
            StrengthVolume = sets.Sum(x => x.Weight * x.Reps)
        };
    }

    private YearlyAnalytics BuildYear(int year, List<WorkoutRow> workouts, List<PplSessionRow> ppl, List<AdditionalRow> additional)
    {
        var sets = workouts.SelectMany(x => x.Sets).Concat(ppl.SelectMany(x => x.Exercises).SelectMany(x => x.Sets)).Concat(additional.SelectMany(x => x.StrengthSets)).ToList();
        var cardio = additional.SelectMany(x => x.Cardio).ToList();
        var distance = cardio.Where(x => x.Unit is CardioUnit.Miles or CardioUnit.Kilometers or CardioUnit.Meters or CardioUnit.Yards).ToList();
        var distanceUnit = distance.GroupBy(x => x.Unit).OrderByDescending(g => g.Sum(x => x.Quantity)).Select(g => (CardioUnit?)g.Key).FirstOrDefault();
        return new YearlyAnalytics
        {
            Year = year, ProgrammedWorkouts = workouts.Count, PplSessions = ppl.Count,
            AdditionalStrengthSessions = additional.Count(x => x.Type == SessionType.CustomStrength),
            CardioSessions = additional.Count(x => x.Type == SessionType.Cardio),
            CompletedSets = sets.Count, TotalReps = sets.Sum(x => x.Reps), StrengthVolume = sets.Sum(x => x.Weight * x.Reps),
            CardioMinutes = cardio.Where(x => x.Unit == CardioUnit.Minutes).Sum(x => x.Quantity) + cardio.Where(x => x.Unit == CardioUnit.Hours).Sum(x => x.Quantity * 60),
            CardioDistance = distance.Where(x => x.Unit == distanceUnit).Sum(x => x.Quantity), CardioDistanceUnit = distanceUnit
        };
    }

    private static List<MonthlyTrainingPoint> BuildMonthly(int year, List<WorkoutRow> workouts, List<PplSessionRow> ppl, List<AdditionalRow> additional)
    {
        return Enumerable.Range(1, 12).Select(month =>
        {
            var dates = workouts.Where(x => x.Date.Year == year && x.Date.Month == month).Select(x => x.Date).Concat(ppl.Where(x => x.Date.Year == year && x.Date.Month == month).Select(x => x.Date)).Concat(additional.Where(x => x.Date.Year == year && x.Date.Month == month).Select(x => x.Date));
            var sets = workouts.Where(x => x.Date.Year == year && x.Date.Month == month).SelectMany(x => x.Sets).Concat(ppl.Where(x => x.Date.Year == year && x.Date.Month == month).SelectMany(x => x.Exercises).SelectMany(x => x.Sets)).Concat(additional.Where(x => x.Date.Year == year && x.Date.Month == month).SelectMany(x => x.StrengthSets));
            return new MonthlyTrainingPoint { Month = month, Label = new DateTime(year, month, 1).ToString("MMM"), Sessions = dates.Count(), StrengthVolume = sets.Sum(x => x.Weight * x.Reps), CardioSessions = additional.Count(x => x.Date.Year == year && x.Date.Month == month && x.Type == SessionType.Cardio) };
        }).ToList();
    }

    private List<StrengthAnalytics> BuildStrengthProgress(List<WorkoutRow> workouts, List<PplSessionRow> ppl, DateTime today, int? currentCycleId)
    {
        var observations = workouts.SelectMany(w => w.Sets.Select(s => new Observation { Key = $"531:{w.LiftType}", Name = s.LiftName, Date = w.Date, CycleId = w.CycleId, Value = weightCalculator.CalculateEstimated1RM(s.Weight, s.Reps) }))
            .Concat(ppl.SelectMany(s => s.Exercises.SelectMany(e => e.Sets.Select(x => new Observation { Key = $"ppl:{e.Key}", Name = e.Name, Date = s.Date, Value = weightCalculator.CalculateEstimated1RM(x.Weight, x.Reps) })))).GroupBy(x => x.Key);
        return observations.Select(group =>
        {
            var items = group.OrderBy(x => x.Date).ToList();
            return new StrengthAnalytics
            {
                ExerciseKey = group.Key, ExerciseName = items.Last().Name, Current = items.Last().Value,
                PreviousCycle = currentCycleId is null ? null : items.Where(x => x.CycleId is not null && x.CycleId < currentCycleId).Select(x => (double?)x.Value).LastOrDefault(),
                ThreeMonthsAgo = AtOrBefore(items, today.AddMonths(-3)), SixMonthsAgo = AtOrBefore(items, today.AddMonths(-6)), OneYearAgo = AtOrBefore(items, today.AddYears(-1)),
                History = items.Select(x => new StrengthPoint { Date = x.Date, Value = x.Value }).ToList()
            };
        }).OrderBy(x => x.ExerciseName).ToList();
    }

    private static List<PerformanceRecord> BuildRecords(List<WorkoutRow> workouts, List<PplSessionRow> ppl)
        => workouts.SelectMany(w => w.Sets.Select(s => new PerformanceRecord { ExerciseName = s.LiftName, Date = w.Date, Weight = s.Weight, Reps = s.Reps }))
            .Concat(ppl.SelectMany(s => s.Exercises.SelectMany(e => e.Sets.Select(x => new PerformanceRecord { ExerciseName = e.Name, Date = s.Date, Weight = x.Weight, Reps = x.Reps })))).OrderByDescending(x => x.Date).Take(5).ToList();

    private static double? AtOrBefore(List<Observation> items, DateTime date) => items.Where(x => x.Date.Date <= date.Date).Select(x => (double?)x.Value).LastOrDefault();
    private static bool InYear(DateTime date, DateTime start) => date >= start && date < start.AddYears(1);
    private static DateTime StartOfWeek(DateTime date) => date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday).Date;

    private sealed class CurrentCycleRow { public int Id { get; init; } public string Name { get; init; } = ""; public string? CurrentWeek { get; init; } public int Planned { get; init; } public int Completed { get; init; } public int Additional { get; init; } }
    private sealed class WorkoutRow { public DateTime Date { get; init; } public int CycleId { get; init; } public int CycleNumber { get; init; } public LiftType LiftType { get; init; } public List<ActualSetRow> Sets { get; init; } = []; }
    private sealed class PplSessionRow { public DateTime Date { get; init; } public List<PplExerciseRow> Exercises { get; init; } = []; }
    private sealed class PplExerciseRow { public string Key { get; init; } = ""; public string Name { get; init; } = ""; public List<ActualSetRow> Sets { get; init; } = []; }
    private sealed class AdditionalRow { public DateTime Date { get; init; } public SessionType Type { get; init; } public List<CardioRow> Cardio { get; init; } = []; public List<ActualSetRow> StrengthSets { get; init; } = []; }
    private sealed class ActualSetRow { public double Weight { get; init; } public int Reps { get; init; } public string LiftName { get; init; } = ""; }
    private sealed class CardioRow { public double Quantity { get; init; } public CardioUnit Unit { get; init; } }
    private sealed class Observation { public string Key { get; init; } = ""; public string Name { get; init; } = ""; public DateTime Date { get; init; } public int? CycleId { get; init; } public double Value { get; init; } }
}
