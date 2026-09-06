using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public class Accessory
{
    public int Id { get; set; }

    /// <summary>Owner user ID. Null = globally-seeded (visible to all users).</summary>
    public string? UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public AccessoryCategory Category { get; set; } = AccessoryCategory.Other;

    public bool IsActive { get; set; } = true;

    public ICollection<WorkoutAccessory> WorkoutAccessories { get; set; } = [];
    public ICollection<AccessoryHistory> History { get; set; } = [];
}
