namespace FiveThreeOneTracker.Models;

public class UserEquipment
{
    public int Id { get; set; }

    /// <summary>Owner user ID (Identity).</summary>
    public string? UserId { get; set; }

    public double BarWeight { get; set; } = 45;

    public ICollection<PlateInventory> Plates { get; set; } = [];
}
