using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class PplSessionExercise
{
    public int Id { get; set; }

    public int PplSessionId { get; set; }
    public PplSession Session { get; set; } = null!;

    public int PplExerciseSlotId { get; set; }
    public PplExerciseSlot ExerciseSlot { get; set; } = null!;

    /// <summary>Snapshot of the exercise name at time of session creation.</summary>
    [Required]
    [StringLength(100)]
    public string ExerciseName { get; set; } = string.Empty;

    public int TargetSets { get; set; }

    public int RepsMin { get; set; }

    public int RepsMax { get; set; }

    /// <summary>Weight suggested at session creation (TM-derived or CurrentWeight).</summary>
    public double SuggestedWeight { get; set; }

    public int OrderInSession { get; set; }

    public bool IsCompleted => Sets.Count > 0 && Sets.All(s => s.IsCompleted);

    public ICollection<PplSessionSet> Sets { get; set; } = [];
}
