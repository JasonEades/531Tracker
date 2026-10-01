namespace FiveThreeOneTracker.Models;

public class PplSession
{
    public int Id { get; set; }

    public int PplProgramId { get; set; }
    public PplProgram Program { get; set; } = null!;

    public int PplDayTemplateId { get; set; }
    public PplDayTemplate DayTemplate { get; set; } = null!;

    public WorkoutStatus Status { get; set; } = WorkoutStatus.NotStarted;

    public int RestSeconds { get; set; } = 90;

    public int? PplWeekId { get; set; }
    public PplWeek? Week { get; set; }

    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<PplSessionExercise> Exercises { get; set; } = [];
}
