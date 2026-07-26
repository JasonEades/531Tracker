namespace FiveThreeOneTracker.Models;

public class WorkoutAccessory
{
    public int Id { get; set; }

    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;

    public int AccessoryId { get; set; }
    public Accessory Accessory { get; set; } = null!;

    public double Weight { get; set; }

    public int Reps { get; set; }

    public int Sets { get; set; }

    public bool IsCompleted { get; set; }
}
