using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public sealed class DailyStepRecord
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public DateTime LocalDate { get; set; }
    public long StepCount { get; set; }

    [Required, StringLength(50)]
    public string Provider { get; set; } = "GoogleHealth";

    [Required, StringLength(100)]
    public string Metric { get; set; } = "DailySteps";

    [StringLength(255)]
    public string? ProviderRecordId { get; set; }

    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public CardioEntry? CardioEntry { get; set; }
}
