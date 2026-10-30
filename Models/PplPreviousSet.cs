namespace FiveThreeOneTracker.Models;

/// <summary>A completed set from the most recent earlier session of the same exercise.</summary>
public sealed record PplPreviousSet(int SetNumber, int Reps, double? Weight, DateTime OccurredOn);
