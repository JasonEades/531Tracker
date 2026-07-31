using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class PplDayTemplate
{
    public int Id { get; set; }

    public int PplProgramId { get; set; }
    public PplProgram Program { get; set; } = null!;

    public PplDayType DayType { get; set; }

    public PplVariant Variant { get; set; } = PplVariant.Single;

    /// <summary>1-based position in the weekly rotation (e.g. 1=first day, 6=last day of 6-day split).</summary>
    public int OrderInWeek { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<PplExerciseSlot> ExerciseSlots { get; set; } = [];
    public ICollection<PplSession> Sessions { get; set; } = [];
}
