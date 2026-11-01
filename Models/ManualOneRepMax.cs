using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

/// <summary>
/// A 1RM the user entered by hand (e.g. from before they tracked in the app). Feeds progress charts, exercise
/// history, and personal records, but never working-weight suggestions.
/// </summary>
public sealed class ManualOneRepMax
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(100)]
    public string ExerciseName { get; set; } = string.Empty;

    /// <summary>The one-rep max in pounds.</summary>
    public double Weight { get; set; }

    public DateTime AchievedOn { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
