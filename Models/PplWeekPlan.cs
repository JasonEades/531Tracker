namespace FiveThreeOneTracker.Models;

public enum PplDayState
{
    /// <summary>Not yet trained this week.</summary>
    Available,
    /// <summary>An open session exists for this day.</summary>
    Open,
    Completed
}

public sealed record PplWeekDay(
    int DayTemplateId,
    int OrderInWeek,
    string Name,
    PplDayType DayType,
    PplDayState State,
    int? SessionId,
    bool HasLoggedSets);

/// <summary>The current PPL week: every scheduled day, its status, and the day suggested next.</summary>
public sealed record PplWeekPlan(int WeekNumber, IReadOnlyList<PplWeekDay> Days, int? SuggestedDayTemplateId)
{
    public PplWeekDay? Suggested => Days.FirstOrDefault(d => d.DayTemplateId == SuggestedDayTemplateId);
    public PplWeekDay? OpenDay => Days.FirstOrDefault(d => d.State == PplDayState.Open);
}
