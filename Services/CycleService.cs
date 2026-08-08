using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface ICycleService
{
    Task<Cycle?> GetCurrentCycleAsync();
    Task<Cycle?> GetCycleWithDetailsAsync(int cycleId);
    Task<List<Cycle>> GetAllCyclesAsync();
    Task<Cycle> CreateCycleAsync(BbbMode bbbMode = BbbMode.None, double bbbPercentage = 50, bool includeWarmup = false,
        bool isFivesPro = false, bool includeFsl = false);
    Task<Cycle> CreateNextCycleAsync(int previousCycleId, Dictionary<LiftType, double>? overrides = null,
        BbbMode? bbbMode = null, double? bbbPercentage = null, bool? includeWarmup = null,
        bool? isFivesPro = null, bool? includeFsl = null);
    Task CompleteCycleAsync(int cycleId);
    Task DeleteCycleAsync(int cycleId);
}

public class CycleService(
    AppDbContext db,
    ILiftService liftService,
    IBbbMappingService bbbMapping,
    IWeightCalculator weightCalc,
    ICurrentUserService userContext) : ICycleService
{
    public async Task<Cycle?> GetCurrentCycleAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Cycles
            .Include(c => c.Weeks)
                .ThenInclude(w => w.Workouts)
                    .ThenInclude(wo => wo.Sets)
            .Include(c => c.Weeks)
                .ThenInclude(w => w.Workouts)
                    .ThenInclude(wo => wo.WorkoutAccessories)
                        .ThenInclude(wa => wa.Accessory)
            .Where(c => !c.IsCompleted && c.UserId == userId)
            .OrderByDescending(c => c.CycleNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<Cycle?> GetCycleWithDetailsAsync(int cycleId)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Cycles
            .Include(c => c.Weeks)
                .ThenInclude(w => w.Workouts)
                    .ThenInclude(wo => wo.Sets)
                        .ThenInclude(s => s.Lift)
            .Include(c => c.Weeks)
                .ThenInclude(w => w.Workouts)
                    .ThenInclude(wo => wo.WorkoutAccessories)
                        .ThenInclude(wa => wa.Accessory)
            .FirstOrDefaultAsync(c => c.Id == cycleId && c.UserId == userId);
    }

    public async Task<List<Cycle>> GetAllCyclesAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Cycles
            .Include(c => c.Weeks)
                .ThenInclude(w => w.Workouts)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CycleNumber)
            .ToListAsync();
    }

    public async Task<Cycle> CreateCycleAsync(BbbMode bbbMode = BbbMode.None, double bbbPercentage = 50, bool includeWarmup = false,
        bool isFivesPro = false, bool includeFsl = false)
    {
        var userId = await userContext.GetUserIdAsync();
        var lastCycle = await db.Cycles
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CycleNumber)
            .FirstOrDefaultAsync();
        var cycleNumber = (lastCycle?.CycleNumber ?? 0) + 1;

        var cycle = new Cycle
        {
            UserId = userId,
            CycleNumber = cycleNumber,
            Name = $"Cycle {cycleNumber}",
            CreatedAt = DateTime.UtcNow,
            BbbMode = bbbMode,
            BbbPercentage = Math.Clamp(bbbPercentage, 30, 70),
            IncludeWarmup = includeWarmup,
            IsFivesPro = isFivesPro,
            IncludeFsl = includeFsl
        };

        db.Cycles.Add(cycle);
        await db.SaveChangesAsync();

        await GenerateWeeksAndWorkoutsAsync(cycle);

        return cycle;
    }

    public async Task<Cycle> CreateNextCycleAsync(int previousCycleId, Dictionary<LiftType, double>? overrides = null,
        BbbMode? bbbMode = null, double? bbbPercentage = null, bool? includeWarmup = null,
        bool? isFivesPro = null, bool? includeFsl = null)
    {
        var previousCycle = await db.Cycles.FindAsync(previousCycleId);
        var lifts = await liftService.GetAllLiftsAsync();

        foreach (var lift in lifts)
        {
            if (overrides is not null && overrides.TryGetValue(lift.LiftType, out var overrideMax))
            {
                await liftService.UpdateTrainingMaxAsync(lift.Id, overrideMax);
            }
            else
            {
                var increment = lift.IsUpperBody ? 5.0 : 10.0;
                await liftService.UpdateTrainingMaxAsync(lift.Id, lift.TrainingMax + increment);
            }
        }

        var mode    = bbbMode       ?? previousCycle?.BbbMode       ?? BbbMode.None;
        var pct     = bbbPercentage ?? previousCycle?.BbbPercentage ?? 50;
        var warmup  = includeWarmup ?? previousCycle?.IncludeWarmup  ?? false;
        var fivesPro = isFivesPro   ?? previousCycle?.IsFivesPro     ?? false;
        var fsl     = includeFsl    ?? previousCycle?.IncludeFsl     ?? false;

        return await CreateCycleAsync(mode, pct, warmup, fivesPro, fsl);
    }

    public async Task CompleteCycleAsync(int cycleId)
    {
        var userId = await userContext.GetUserIdAsync();
        var cycle = await db.Cycles.FirstOrDefaultAsync(c => c.Id == cycleId && c.UserId == userId);
        if (cycle is not null)
        {
            cycle.IsCompleted = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteCycleAsync(int cycleId)
    {
        var userId = await userContext.GetUserIdAsync();
        var cycle = await db.Cycles
            .Include(c => c.Weeks)
                .ThenInclude(w => w.Workouts)
                    .ThenInclude(wo => wo.Sets)
            .Include(c => c.Weeks)
                .ThenInclude(w => w.Workouts)
                    .ThenInclude(wo => wo.WorkoutAccessories)
            .FirstOrDefaultAsync(c => c.Id == cycleId && c.UserId == userId);

        if (cycle is not null)
        {
            db.Cycles.Remove(cycle);
            await db.SaveChangesAsync();
        }
    }

    private async Task GenerateWeeksAndWorkoutsAsync(Cycle cycle)
    {
        var lifts = await liftService.GetAllLiftsAsync();
        var liftDict = lifts.ToDictionary(l => l.LiftType);

        foreach (WeekNumber weekNum in Enum.GetValues<WeekNumber>())
        {
            var week = new Week
            {
                CycleId = cycle.Id,
                WeekNumber = weekNum
            };
            db.Weeks.Add(week);
            await db.SaveChangesAsync();

            foreach (var mainLift in lifts)
            {
                var workout = new Workout
                {
                    WeekId = week.Id,
                    MainLiftType = mainLift.LiftType,
                    Status = WorkoutStatus.NotStarted
                };
                db.Workouts.Add(workout);
                await db.SaveChangesAsync();

                var setNumber = 1;

                if (cycle.IncludeWarmup)
                {
                    var warmupSets = weightCalc.GetWarmupSets();
                    foreach (var (percentage, reps) in warmupSets)
                    {
                        db.WorkoutSets.Add(new WorkoutSet
                        {
                            WorkoutId = workout.Id,
                            LiftId = mainLift.Id,
                            SetType = SetType.Warmup,
                            SetNumber = setNumber++,
                            PrescribedWeight = weightCalc.CalculateWeight(mainLift.TrainingMax, percentage),
                            PrescribedReps = reps
                        });
                    }
                }

                var mainSetScheme = cycle.IsFivesPro
                    ? weightCalc.GetFivesProMainSets(weekNum)
                    : weightCalc.GetMainSets(weekNum);
                double firstSetWeight = 0;
                foreach (var (percentage, reps) in mainSetScheme)
                {
                    var w = weightCalc.CalculateWeight(mainLift.TrainingMax, percentage);
                    if (firstSetWeight == 0) firstSetWeight = w;
                    db.WorkoutSets.Add(new WorkoutSet
                    {
                        WorkoutId = workout.Id,
                        LiftId = mainLift.Id,
                        SetType = SetType.Main,
                        SetNumber = setNumber++,
                        PrescribedWeight = w,
                        PrescribedReps = reps
                    });
                }

                if (cycle.IncludeFsl && weekNum != WeekNumber.Week4)
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        db.WorkoutSets.Add(new WorkoutSet
                        {
                            WorkoutId = workout.Id,
                            LiftId = mainLift.Id,
                            SetType = SetType.Fsl,
                            SetNumber = i,
                            PrescribedWeight = firstSetWeight,
                            PrescribedReps = 5
                        });
                    }
                }

                if (cycle.HasBbb)
                {
                    var bbbLiftType = bbbMapping.GetBbbLiftType(mainLift.LiftType, cycle.BbbMode);
                    var bbbLift = liftDict[bbbLiftType];
                    var bbbWeight = weightCalc.CalculateBbbWeight(bbbLift.TrainingMax, cycle.BbbPercentage);

                    for (int i = 1; i <= 5; i++)
                    {
                        db.WorkoutSets.Add(new WorkoutSet
                        {
                            WorkoutId = workout.Id,
                            LiftId = bbbLift.Id,
                            SetType = SetType.Bbb,
                            SetNumber = i,
                            PrescribedWeight = bbbWeight,
                            PrescribedReps = 10
                        });
                    }
                }

                await db.SaveChangesAsync();
            }
        }
    }
}
