using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IUserInitService
{
    /// <summary>
    /// Ensures the user has their 4 default lifts and a default equipment record.
    /// Legacy unowned data is claimed only when this is the installation's sole account.
    /// Safe to call on every login — idempotent.
    /// </summary>
    Task EnsureUserInitializedAsync(string userId);
}

public class UserInitService(AppDbContext db, IEstimatedStrengthService estimatedStrength, IWeightCalculator weightCalculator) : IUserInitService
{
    private static readonly (LiftType Type, string Name, double Tm, double BbbPct)[] DefaultLifts =
    [
        (LiftType.Squat,         "Squat",          315, 50),
        (LiftType.BenchPress,    "Bench Press",     225, 50),
        (LiftType.Deadlift,      "Deadlift",        365, 50),
        (LiftType.OverheadPress, "Overhead Press",  145, 50),
    ];

    public async Task EnsureUserInitializedAsync(string userId)
    {
        var canClaimLegacyData = await db.Users.CountAsync() == 1;
        var hasLifts = await db.Lifts.AnyAsync(l => l.UserId == userId);
        var initializeLiftMaxes = false;
        if (!hasLifts)
        {
            // Claim legacy unowned data only while this is a single-account installation.
            var orphanedLifts = canClaimLegacyData
                ? await db.Lifts.Where(l => l.UserId == null).ToListAsync()
                : [];
            if (orphanedLifts.Count > 0)
            {
                foreach (var lift in orphanedLifts)
                    lift.UserId = userId;
                initializeLiftMaxes = true;
            }
            else
            {
                // New user with no existing data — create defaults
                foreach (var (type, name, tm, bbb) in DefaultLifts)
                {
                    db.Lifts.Add(new Lift
                    {
                        UserId        = userId,
                        LiftType      = type,
                        Name          = name,
                        TrainingMax   = tm,
                        BbbPercentage = bbb,
                    });
                }
                initializeLiftMaxes = true;
            }
        }

        var hasEquipment = await db.UserEquipment.AnyAsync(e => e.UserId == userId);
        if (!hasEquipment)
        {
            // Claim legacy unowned equipment only on a single-account installation.
            var orphanedEquipment = canClaimLegacyData
                ? await db.UserEquipment.Where(e => e.UserId == null).ToListAsync()
                : [];
            if (orphanedEquipment.Count > 0)
            {
                foreach (var eq in orphanedEquipment)
                    eq.UserId = userId;
            }
            else
            {
                var equipment = new UserEquipment { UserId = userId };
                equipment.Plates.Add(new PlateInventory { Weight = 45,  PairsAvailable = 2 });
                equipment.Plates.Add(new PlateInventory { Weight = 35,  PairsAvailable = 2 });
                equipment.Plates.Add(new PlateInventory { Weight = 25,  PairsAvailable = 2 });
                equipment.Plates.Add(new PlateInventory { Weight = 10,  PairsAvailable = 4 });
                equipment.Plates.Add(new PlateInventory { Weight = 5,   PairsAvailable = 4 });
                equipment.Plates.Add(new PlateInventory { Weight = 2.5, PairsAvailable = 4 });
                db.UserEquipment.Add(equipment);
            }
        }

        var hasBars = await db.Bars.AnyAsync(b => b.UserId == userId);
        if (!hasBars)
        {
            var orphanedBars = canClaimLegacyData
                ? await db.Bars.Where(b => b.UserId == null).ToListAsync()
                : [];
            if (orphanedBars.Count > 0)
            {
                foreach (var bar in orphanedBars)
                    bar.UserId = userId;
            }
            else
            {
                db.Bars.Add(new Bar { UserId = userId, Name = "Standard Olympic", Weight = 45, IsDefault = true });
            }
        }

        // Claim any unowned cycles (and their entire workout tree via cascade FK)
        var orphanedCycles = canClaimLegacyData
            ? await db.Cycles.Where(c => c.UserId == null).ToListAsync()
            : [];
        foreach (var cycle in orphanedCycles)
            cycle.UserId = userId;

        // Claim any unowned PPL programs
        var orphanedPpl = canClaimLegacyData
            ? await db.PplPrograms.Where(p => p.UserId == null).ToListAsync()
            : [];
        foreach (var ppl in orphanedPpl)
            ppl.UserId = userId;

        await db.SaveChangesAsync();

        if (initializeLiftMaxes)
        {
            var profile = await estimatedStrength.GetProfileAsync(userId);
            var lifts = await db.Lifts.Where(l => l.UserId == userId).ToListAsync();
            foreach (var lift in lifts)
            {
                if (profile.MainLiftOneRepMaxes.TryGetValue(lift.LiftType, out var oneRepMax))
                    lift.TrainingMax = weightCalculator.RoundToNearest5(oneRepMax * 0.9);
            }

            await db.SaveChangesAsync();
        }
    }
}
