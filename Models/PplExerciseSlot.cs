using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class PplExerciseSlot
{
    public int Id { get; set; }

    public int PplDayTemplateId { get; set; }
    public PplDayTemplate DayTemplate { get; set; } = null!;

    public int OrderInDay { get; set; }

    [Required]
    [StringLength(100)]
    public string ExerciseName { get; set; } = string.Empty;

    public MuscleGroup MuscleGroup { get; set; }

    public int TargetSets { get; set; } = 3;

    public int RepsMin { get; set; } = 8;

    public int RepsMax { get; set; } = 12;

    /// <summary>When true the working weight is derived from TrainingMax × TmPercentage.</summary>
    public bool UsePercentageOfTm { get; set; }

    /// <summary>Fraction of TM (e.g. 0.75 = 75%). Only used when UsePercentageOfTm is true.</summary>
    public double TmPercentage { get; set; }

    /// <summary>FK to Lift for TM-based exercises. Null for isolation work.</summary>
    public int? LiftId { get; set; }
    public Lift? Lift { get; set; }

    /// <summary>Current working weight for double-progression exercises. Updated automatically on progression.</summary>
    public double CurrentWeight { get; set; }

    /// <summary>Pounds to add when the progression threshold is met.</summary>
    public double ProgressionIncrement { get; set; } = 5;

    public bool IsBodyweight { get; set; }

    public ICollection<PplSessionExercise> SessionExercises { get; set; } = [];
}
