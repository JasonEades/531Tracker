using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class ExerciseDefinition
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public MuscleGroup PrimaryMuscle { get; set; }

    [StringLength(200)]
    public string SecondaryMuscles { get; set; } = string.Empty;

    [StringLength(100)]
    public string Equipment { get; set; } = string.Empty;

    [StringLength(60)]
    public string MovementType { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Instructions { get; set; } = string.Empty;

    /// <summary>Null for the curated catalog; set for an account-owned custom exercise.</summary>
    [StringLength(450)]
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
