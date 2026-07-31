using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class PplProgram
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>3, 4, or 6 days per week.</summary>
    public int DaysPerWeek { get; set; } = 3;

    public bool IsActive { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public ICollection<PplDayTemplate> DayTemplates { get; set; } = [];
    public ICollection<PplSession> Sessions { get; set; } = [];
}
