namespace FiveThreeOneTracker.Models;

public class AccessoryHistory
{
    public int Id { get; set; }

    public int AccessoryId { get; set; }
    public Accessory Accessory { get; set; } = null!;

    public double Weight { get; set; }

    public int Reps { get; set; }

    public int Sets { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
