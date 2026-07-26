using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class Lift
{
    public int Id { get; set; }

    public LiftType LiftType { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public double TrainingMax { get; set; }

    public double BbbPercentage { get; set; } = 50;

    public bool IsUpperBody => LiftType is LiftType.BenchPress or LiftType.OverheadPress;

    public ICollection<WorkoutSet> WorkoutSets { get; set; } = [];
}
