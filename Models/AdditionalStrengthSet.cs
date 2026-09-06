namespace FiveThreeOneTracker.Models;

public class AdditionalStrengthSet
{
    public int Id { get; set; }
    public int AdditionalStrengthExerciseId { get; set; }
    public AdditionalStrengthExercise Exercise { get; set; } = null!;
    public int SetNumber { get; set; }
    public double? Weight { get; set; }
    public int? Reps { get; set; }
    public string? Notes { get; set; }
}