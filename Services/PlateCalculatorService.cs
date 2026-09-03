using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IPlateCalculatorService
{
    Task<UserEquipment> GetEquipmentAsync();
    Task UpdatePlateAsync(int plateId, int pairs);
    Task<List<PlateLoadingResult>> CalculatePlates(double targetWeight, double? barWeightOverride = null);

    Task<List<Bar>> GetBarsAsync();
    Task<Bar> GetDefaultBarAsync();
    Task<Bar> AddBarAsync(string name, double weight);
    Task UpdateBarAsync(int barId, string name, double weight);
    Task DeleteBarAsync(int barId);
    Task SetDefaultBarAsync(int barId);
}

public class PlateLoadingResult
{
    public double PlateWeight { get; set; }
    public int CountPerSide { get; set; }
}

public class PlateCalculatorService(AppDbContext db, ICurrentUserService userContext) : IPlateCalculatorService
{
    private static readonly double[] DefaultPlateSizes = [45, 35, 25, 15, 10, 5, 2.5];
    private static readonly Dictionary<double, int> DefaultPairs = new()
    {
        [45] = 4, [35] = 2, [25] = 4, [15] = 2, [10] = 4, [5] = 4, [2.5] = 2
    };

    public async Task<UserEquipment> GetEquipmentAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        var equipment = await db.UserEquipment
            .Include(e => e.Plates.OrderByDescending(p => p.Weight))
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (equipment is null)
        {
            equipment = new UserEquipment { UserId = userId };
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

    public async Task UpdatePlateAsync(int plateId, int pairs)
    {
        var plate = await db.PlateInventory.FindAsync(plateId);
        if (plate is not null)
        {
            plate.PairsAvailable = Math.Max(0, pairs);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<Bar>> GetBarsAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        var bars = await db.Bars.Where(b => b.UserId == userId).OrderByDescending(b => b.IsDefault).ThenBy(b => b.Weight).ToListAsync();

        if (bars.Count == 0)
        {
            var defaultBar = new Bar { UserId = userId, Name = "Standard Olympic", Weight = 45, IsDefault = true };
            db.Bars.Add(defaultBar);
            await db.SaveChangesAsync();
            bars.Add(defaultBar);
        }

        return bars;
    }

    public async Task<Bar> GetDefaultBarAsync()
    {
        var bars = await GetBarsAsync();
        return bars.FirstOrDefault(b => b.IsDefault) ?? bars[0];
    }

    public async Task<Bar> AddBarAsync(string name, double weight)
    {
        var userId = await userContext.GetUserIdAsync();
        var hasAny = await db.Bars.AnyAsync(b => b.UserId == userId);
        var bar = new Bar { UserId = userId, Name = name, Weight = weight, IsDefault = !hasAny };
        db.Bars.Add(bar);
        await db.SaveChangesAsync();
        return bar;
    }

    public async Task UpdateBarAsync(int barId, string name, double weight)
    {
        var bar = await db.Bars.FindAsync(barId);
        if (bar is not null)
        {
            bar.Name = name;
            bar.Weight = weight;
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteBarAsync(int barId)
    {
        var userId = await userContext.GetUserIdAsync();
        var bar = await db.Bars.FindAsync(barId);
        if (bar is null) return;

        var wasDefault = bar.IsDefault;
        db.Bars.Remove(bar);
        await db.SaveChangesAsync();

        if (wasDefault)
        {
            var remaining = await db.Bars.Where(b => b.UserId == userId).OrderBy(b => b.Id).FirstOrDefaultAsync();
            if (remaining is not null)
            {
                remaining.IsDefault = true;
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task SetDefaultBarAsync(int barId)
    {
        var userId = await userContext.GetUserIdAsync();
        var bars = await db.Bars.Where(b => b.UserId == userId).ToListAsync();
        foreach (var bar in bars)
            bar.IsDefault = bar.Id == barId;
        await db.SaveChangesAsync();
    }

    public async Task<List<PlateLoadingResult>> CalculatePlates(double targetWeight, double? barWeightOverride = null)
    {
        var equipment = await GetEquipmentAsync();
        var barWeight = barWeightOverride ?? (await GetDefaultBarAsync()).Weight;
        var results = new List<PlateLoadingResult>();

        var remaining = targetWeight - barWeight;
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
