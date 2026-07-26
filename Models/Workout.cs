namespace FiveThreeOneTracker.Models;

public class Workout
{
    public int Id { get; set; }

    public int WeekId { get; set; }
    public Week Week { get; set; } = null!;

    public LiftType MainLiftType { get; set; }

    public WorkoutStatus Status { get; set; } = WorkoutStatus.NotStarted;

    public DateTime? CompletedAt { get; set; }

    public ICollection<WorkoutSet> Sets { get; set; } = [];

    public ICollection<WorkoutAccessory> WorkoutAccessories { get; set; } = [];
}
