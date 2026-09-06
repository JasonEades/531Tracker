using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class AdditionalStrengthExercise
{
    public int Id { get; set; }
    public int AdditionalSessionId { get; set; }
    public AdditionalSession Session { get; set; } = null!;

    [Required, StringLength(100)]
    public string ExerciseName { get; set; } = string.Empty;
    public int Order { get; set; }
    public ICollection<AdditionalStrengthSet> Sets { get; set; } = [];
}