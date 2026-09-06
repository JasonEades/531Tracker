namespace FiveThreeOneTracker.Models;

public class WorkoutSet
{
    public int Id { get; set; }

    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;

    public int LiftId { get; set; }
    public Lift Lift { get; set; } = null!;

    public SetType SetType { get; set; }

    public int SetNumber { get; set; }

    public bool IsAdditional { get; set; }

    public AdditionalSetType AdditionalSetType { get; set; } = AdditionalSetType.Additional;

    public int Sequence { get; set; }

    public double PrescribedWeight { get; set; }

    public int PrescribedReps { get; set; }

    public double? ActualWeight { get; set; }

    public int? ActualReps { get; set; }

    public double? Rpe { get; set; }

    public double? Rir { get; set; }

    public string? Notes { get; set; }

    public bool IsCompleted { get; set; }
}
