namespace FiveThreeOneTracker.Models;

public sealed class AccessoryUsageSuggestion
{
    public double Weight { get; init; }
    public int Reps { get; init; }
    public int Sets { get; init; }
    public string? Notes { get; init; }
}
