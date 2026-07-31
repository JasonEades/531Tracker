namespace FiveThreeOneTracker.Models;

public class PplSessionSet
{
    public int Id { get; set; }

    public int PplSessionExerciseId { get; set; }
    public PplSessionExercise SessionExercise { get; set; } = null!;

    public int SetNumber { get; set; }

    public int TargetReps { get; set; }

    public int? ActualReps { get; set; }

    public double? ActualWeight { get; set; }

    public bool IsCompleted { get; set; }
}
