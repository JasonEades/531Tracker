using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class PplSessionExercise
{
    public int Id { get; set; }

    public int PplSessionId { get; set; }
    public PplSession Session { get; set; } = null!;

    /// <summary>The plan exercise this came from. Null for an exercise added to just this session.</summary>
    public int? PplExerciseSlotId { get; set; }
    public PplExerciseSlot? ExerciseSlot { get; set; }

    /// <summary>Muscle group of a session-only exercise (plan exercises use their slot's).</summary>
    public MuscleGroup? AddedMuscleGroup { get; set; }

    /// <summary>Whether a session-only exercise is bodyweight (plan exercises use their slot's).</summary>
    public bool AddedIsBodyweight { get; set; }

    public bool IsAddedToSession => PplExerciseSlotId is null && ExerciseSlot is null;

    public MuscleGroup? EffectiveMuscleGroup => ExerciseSlot?.MuscleGroup ?? AddedMuscleGroup;

    public bool EffectiveIsBodyweight => ExerciseSlot?.IsBodyweight ?? AddedIsBodyweight;

    /// <summary>True when a plan exercise was swapped for a different one in this session only.</summary>
    public bool IsOneOffSwap => ExerciseSlot is not null
        && !string.Equals(ExerciseName, ExerciseSlot.ExerciseName, StringComparison.OrdinalIgnoreCase);

    /// <summary>Snapshot of the exercise name at time of session creation.</summary>
    [Required]
    [StringLength(100)]
    public string ExerciseName { get; set; } = string.Empty;

    public int TargetSets { get; set; }

    public int RepsMin { get; set; }

    public int RepsMax { get; set; }

    /// <summary>Weight planned at session creation. Null means the user has not established a starting weight.</summary>
    public double? SuggestedWeight { get; set; }

    public int OrderInSession { get; set; }

    /// <summary>Bar used for plate loading in this session. Null = the user's default bar.</summary>
    public int? BarId { get; set; }
    public Bar? Bar { get; set; }

    public bool IsCompleted => Sets.Count > 0 && Sets.All(s => s.IsCompleted);

    public ICollection<PplSessionSet> Sets { get; set; } = [];
}
