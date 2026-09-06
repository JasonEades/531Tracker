using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class AdditionalSession
{
    public int Id { get; set; }
    public int? WeekId { get; set; }
    public Week? Week { get; set; }
    public int? PplWeekId { get; set; }
    public PplWeek? PplWeek { get; set; }
    public SessionType SessionType { get; set; }
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public WorkoutStatus Status { get; set; } = WorkoutStatus.Completed;

    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Notes { get; set; }
    public ICollection<AdditionalStrengthExercise> StrengthExercises { get; set; } = [];
    public ICollection<CardioEntry> CardioEntries { get; set; } = [];
}