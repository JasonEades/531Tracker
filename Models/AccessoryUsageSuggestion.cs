namespace FiveThreeOneTracker.Models;

/// <summary>The most recent logged working weight for an accessory the user can see.</summary>
public sealed record AccessoryWeightOption(int AccessoryId, string Name, double Weight, int Reps, DateTime LastUsed)
{
    /// <summary>
    /// Working weight for a PPL rep range: the logged weight when its reps already fall in the range,
    /// otherwise an Epley-equivalent load for the top of the range, rounded to 2.5 lb.
    /// </summary>
    public double WeightForRepRange(int repsMin, int repsMax)
    {
        if (Reps >= repsMin && Reps <= repsMax) return Weight;
        var oneRepMax = Weight * (1 + Reps / 30.0);
        var target = oneRepMax / (1 + repsMax / 30.0);
        return Math.Max(2.5, Math.Round(target / 2.5) * 2.5);
    }
}

public sealed class AccessoryUsageSuggestion
{
    public double Weight { get; init; }
    public int Reps { get; init; }
    public int Sets { get; init; }
    public string? Notes { get; init; }
}
