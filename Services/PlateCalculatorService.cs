using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IPlateCalculatorService
{
    Task<UserEquipment> GetEquipmentAsync();
    Task UpdateBarWeightAsync(double barWeight);
    Task UpdatePlateAsync(int plateId, int pairs);
    Task<List<PlateLoadingResult>> CalculatePlates(double targetWeight);
}

public class PlateLoadingResult
{
    public double PlateWeight { get; set; }
    public int CountPerSide { get; set; }
}

public class PlateCalculatorService(AppDbContext db) : IPlateCalculatorService
{
    private static readonly double[] DefaultPlateSizes = [45, 35, 25, 15, 10, 5, 2.5];
    private static readonly Dictionary<double, int> DefaultPairs = new()
    {
        [45] = 4, [35] = 2, [25] = 4, [15] = 2, [10] = 4, [5] = 4, [2.5] = 2
    };

    public async Task<UserEquipment> GetEquipmentAsync()
    {
        var equipment = await db.UserEquipment
            .Include(e => e.Plates.OrderByDescending(p => p.Weight))
            .FirstOrDefaultAsync();

        if (equipment is null)
        {
            equipment = new UserEquipment { BarWeight = 45 };
            db.UserEquipment.Add(equipment);
            await db.SaveChangesAsync();

            var defaultPlates = DefaultPlateSizes.Select(w => new PlateInventory
            {
                UserEquipmentId = equipment.Id,
                Weight = w,
                PairsAvailable = DefaultPairs[w]
            }).ToList();

            db.PlateInventory.AddRange(defaultPlates);
            await db.SaveChangesAsync();

            equipment.Plates = defaultPlates;
        }
        else
        {
            var existingWeights = equipment.Plates.Select(p => p.Weight).ToHashSet();
            var missing = DefaultPlateSizes.Where(w => !existingWeights.Contains(w)).ToList();

            if (missing.Count > 0)
            {
                foreach (var w in missing)
                {
                    var plate = new PlateInventory
                    {
                        UserEquipmentId = equipment.Id,
                        Weight = w,
                        PairsAvailable = 0
                    };
                    db.PlateInventory.Add(plate);
                    equipment.Plates.Add(plate);
                }
                await db.SaveChangesAsync();
            }
        }

        return equipment;
    }

    public async Task UpdateBarWeightAsync(double barWeight)
    {
        var equipment = await db.UserEquipment.FirstOrDefaultAsync();
        if (equipment is not null)
        {
            equipment.BarWeight = barWeight;
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdatePlateAsync(int plateId, int pairs)
    {
        var plate = await db.PlateInventory.FindAsync(plateId);
        if (plate is not null)
        {
            plate.PairsAvailable = Math.Max(0, pairs);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<PlateLoadingResult>> CalculatePlates(double targetWeight)
    {
        var equipment = await GetEquipmentAsync();
        var results = new List<PlateLoadingResult>();

        var remaining = targetWeight - equipment.BarWeight;
        if (remaining <= 0)
            return results;

        var perSide = remaining / 2.0;

        foreach (var plate in equipment.Plates.OrderByDescending(p => p.Weight))
        {
            if (plate.PairsAvailable <= 0 || plate.Weight > perSide)
                continue;

            var count = (int)(perSide / plate.Weight);
            count = Math.Min(count, plate.PairsAvailable);

            if (count > 0)
            {
                results.Add(new PlateLoadingResult
                {
                    PlateWeight = plate.Weight,
                    CountPerSide = count
                });
                perSide -= count * plate.Weight;
            }
        }

        return results;
    }
}
