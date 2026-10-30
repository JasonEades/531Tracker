namespace FiveThreeOneTracker.Models;

/// <summary>
/// A completed set from the most recent earlier session of the same exercise. Source is null for PPL sessions and
/// names the other program (e.g. "5/3/1") when the exercise was last trained outside PPL.
/// </summary>
public sealed record PplPreviousSet(int SetNumber, int Reps, double? Weight, DateTime OccurredOn, string? Source = null);
