using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class Cycle
{
    public int Id { get; set; }

    /// <summary>Owner user ID (Identity).</summary>
    public string? UserId { get; set; }

    public int CycleNumber { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; }

    public BbbMode BbbMode { get; set; } = BbbMode.None;

    public double BbbPercentage { get; set; } = 50;

    public bool HasBbb => BbbMode != BbbMode.None;

    public bool IncludeWarmup { get; set; }

    public bool IsFivesPro { get; set; }

    public bool IncludeFsl { get; set; }

    public ICollection<Week> Weeks { get; set; } = [];
}
