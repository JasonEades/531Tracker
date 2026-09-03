namespace FiveThreeOneTracker.Models;

/// <summary>
/// A named lifting bar with its own weight (e.g. "Standard Olympic — 45 lbs",
/// "Women's Olympic — 35 lbs", "Trap Bar — 60 lbs"). Users can define multiple
/// bars and select one per workout for accurate plate loading.
/// </summary>
public class Bar
{
    public int Id { get; set; }

    /// <summary>Owner user ID (Identity).</summary>
    public string? UserId { get; set; }

    public string Name { get; set; } = "";

    public double Weight { get; set; }

    /// <summary>Used when a workout doesn't explicitly select a bar.</summary>
    public bool IsDefault { get; set; }
}
