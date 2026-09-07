namespace FiveThreeOneTracker.Models;

public sealed class DashboardAnalytics
{
    public CurrentProgramAnalytics Program { get; init; } = new();
    public WeeklyAnalytics ThisWeek { get; init; } = new();
    public YearlyAnalytics YearToDate { get; init; } = new();
    public IReadOnlyList<MonthlyTrainingPoint> MonthlyTraining { get; init; } = [];
    public IReadOnlyList<StrengthAnalytics> StrengthProgress { get; init; } = [];
    public IReadOnlyList<PerformanceRecord> RecentRecords { get; init; } = [];
    public StepAnalytics Steps { get; init; } = new();
}

public sealed class StepAnalytics
{
    public bool IsConnected { get; init; }
    public long Today { get; init; }
    public long ThisWeekTotal { get; init; }
    public double ThisWeekAverage { get; init; }
    public long ThisWeekHighest { get; init; }
    public long ThisWeekLowest { get; init; }
    public int ThisWeekDaysWithData { get; init; }
    public long YearTotal { get; init; }
    public double YearAverage { get; init; }
    public IReadOnlyList<DailyStepPoint> ThisWeek { get; init; } = [];
    public IReadOnlyList<DailyStepPoint> Last30Days { get; init; } = [];
}

public sealed class DailyStepPoint
{
    public DateTime Date { get; init; }
    public long Steps { get; init; }
}

public sealed class CurrentProgramAnalytics
{
    public string? Name { get; init; }
    public string? ProgramType { get; init; }
    public string? CurrentWeek { get; init; }
    public int PlannedSessions { get; init; }
    public int CompletedSessions { get; init; }
    public int CompletionPercent => PlannedSessions == 0 ? 0 : (int)Math.Round(CompletedSessions * 100d / PlannedSessions, MidpointRounding.AwayFromZero);
    public int AdditionalSessions { get; init; }
}

public sealed class WeeklyAnalytics
{
    public int ProgrammedWorkouts { get; init; }
    public int PplSessions { get; init; }
    public int AdditionalSessions { get; init; }
    public int CardioSessions { get; init; }
    public int CompletedSets { get; init; }
    public int TotalReps { get; init; }
    public double StrengthVolume { get; init; }
}

public sealed class YearlyAnalytics
{
    public int Year { get; init; }
    public int ProgrammedWorkouts { get; init; }
    public int PplSessions { get; init; }
    public int AdditionalStrengthSessions { get; init; }
    public int CardioSessions { get; init; }
    public int TotalSessions => ProgrammedWorkouts + PplSessions + AdditionalStrengthSessions + CardioSessions;
    public int CompletedSets { get; init; }
    public int TotalReps { get; init; }
    public double StrengthVolume { get; init; }
    public double CardioMinutes { get; init; }
    public double CardioDistance { get; init; }
    public CardioUnit? CardioDistanceUnit { get; init; }
}

public sealed class MonthlyTrainingPoint
{
    public int Month { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Sessions { get; init; }
    public double StrengthVolume { get; init; }
    public int CardioSessions { get; init; }
}

public sealed class StrengthAnalytics
{
    public string ExerciseKey { get; init; } = string.Empty;
    public string ExerciseName { get; init; } = string.Empty;
    public string MetricName { get; init; } = "Estimated 1RM";
    public double? Current { get; init; }
    public double? PreviousCycle { get; init; }
    public double? ThreeMonthsAgo { get; init; }
    public double? SixMonthsAgo { get; init; }
    public double? OneYearAgo { get; init; }
    public IReadOnlyList<StrengthPoint> History { get; init; } = [];
    public string? ComparisonText => Current is null || SixMonthsAgo is null
        ? null
        : $"{Current.Value - SixMonthsAgo.Value:+#;-#;0} lb vs 6 months ago";
}

public sealed class StrengthPoint
{
    public DateTime Date { get; init; }
    public double Value { get; init; }
}

public sealed class PerformanceRecord
{
    public string ExerciseName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public double Weight { get; init; }
    public int Reps { get; init; }
    public double? Estimated1Rm { get; init; }
}

public static class DashboardMetricDefinitions
{
    public const string CycleCompletion = "Completed programmed workouts / total programmed workouts. Additional sessions are excluded.";
    public const string StrengthVolume = "Sum of actual weight × actual reps for completed strength sets. Planned values are excluded.";
    public const string Estimated1Rm = "The existing rounded Epley formula: weight × (1 + reps / 30), rounded to the nearest 5.";
    public const string HistoricalLookup = "The most recent valid observation at or before the comparison date; missing history remains unavailable.";
    public const string YearlyWorkouts = "Completed programmed 5/3/1 workouts and completed PPL sessions whose OccurredOn date is in the calendar year.";
}
