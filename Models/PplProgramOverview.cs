namespace FiveThreeOneTracker.Models;

/// <summary>Planned workouts for one training week; customized weeks override the program's default schedule.</summary>
public sealed record PplScheduledWeek(int WeekNumber, bool IsCustomized, IReadOnlyList<PplDayTemplate> Days);

/// <summary>Everything the PPL program landing page shows: this week, planned upcoming weeks, and logged weeks.</summary>
public sealed record PplProgramOverview(
    PplProgram Program,
    PplWeekPlan CurrentWeek,
    PplScheduledWeek CurrentSchedule,
    IReadOnlyList<PplScheduledWeek> UpcomingWeeks,
    IReadOnlyList<PplWeek> Weeks,
    IReadOnlyList<PplSession> UnassignedSessions);
