namespace FiveThreeOneTracker.Models;

public class Week
{
    public int Id { get; set; }

    public int CycleId { get; set; }
    public Cycle Cycle { get; set; } = null!;

    public WeekNumber WeekNumber { get; set; }

    public ICollection<Workout> Workouts { get; set; } = [];

    public bool IsCompleted => Workouts.Count > 0 && Workouts.All(w => w.Status == WorkoutStatus.Completed);
}
