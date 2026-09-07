using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class CardioEntry
{
    public int Id { get; set; }
    public int AdditionalSessionId { get; set; }
    public AdditionalSession Session { get; set; } = null!;
    public int AccessoryId { get; set; }
    public Accessory Accessory { get; set; } = null!;
    public double Quantity { get; set; }
    public CardioUnit Unit { get; set; }

    public int? DailyStepRecordId { get; set; }
    public DailyStepRecord? DailyStepRecord { get; set; }

    [StringLength(50)]
    public string? Source { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}