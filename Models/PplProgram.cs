using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class PplProgram
{
    public int Id { get; set; }

    /// <summary>Owner user ID (Identity).</summary>
    public string? UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>3, 4, or 6 days per week.</summary>
    public int DaysPerWeek { get; set; } = 3;

    public bool IsActive { get; set; }

    /// <summary>Set when the user ends the program; inactive programs without it are paused and can be resumed.</summary>
    public DateTime? EndedAt { get; set; }

    public bool IsEnded => EndedAt.HasValue;

    public string StatusLabel => IsActive ? "Active" : IsEnded ? "Ended" : "Paused";

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<PplDayTemplate> DayTemplates { get; set; } = [];
    public ICollection<PplSession> Sessions { get; set; } = [];
    public ICollection<PplWeek> Weeks { get; set; } = [];
}
