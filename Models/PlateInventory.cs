namespace FiveThreeOneTracker.Models;

public class PlateInventory
{
    public int Id { get; set; }

    public int UserEquipmentId { get; set; }

    public double Weight { get; set; }

    public int PairsAvailable { get; set; }

    public UserEquipment UserEquipment { get; set; } = null!;
}
