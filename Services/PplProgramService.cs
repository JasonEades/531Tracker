using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IPplProgramService
{
    Task<PplProgram?> GetActiveProgramAsync();
    Task<List<PplProgram>> GetAllProgramsAsync();
    Task<PplProgram?> GetProgramWithDetailsAsync(int programId);
    Task<PplProgram> CreateProgramAsync(string name, int daysPerWeek, string? notes = null);
    Task SetActiveProgramAsync(int programId);
    Task DeleteProgramAsync(int programId);
    Task<PplSession> GetOrCreateNextSessionAsync(int programId);
}

public class PplProgramService(AppDbContext db, ILiftService liftService, ICurrentUserService userContext) : IPplProgramService
{
    public async Task<PplProgram?> GetActiveProgramAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.PplPrograms.FirstOrDefaultAsync(p => p.IsActive && p.UserId == userId);
    }

    public async Task<List<PplProgram>> GetAllProgramsAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.PplPrograms
            .Include(p => p.Sessions)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<PplProgram?> GetProgramWithDetailsAsync(int programId)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.PplPrograms
            .Include(p => p.DayTemplates.OrderBy(d => d.OrderInWeek))
                .ThenInclude(d => d.ExerciseSlots.OrderBy(s => s.OrderInDay))
                    .ThenInclude(s => s.Lift)
            .FirstOrDefaultAsync(p => p.Id == programId && p.UserId == userId);
    }

    public async Task<PplProgram> CreateProgramAsync(string name, int daysPerWeek, string? notes = null)
    {
        var userId = await userContext.GetUserIdAsync();
        var lifts = await liftService.GetAllLiftsAsync();
        var liftDict = lifts.ToDictionary(l => l.LiftType);

        var program = new PplProgram
        {
            UserId = userId,
            Name = name,
            DaysPerWeek = daysPerWeek,
            Notes = notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Deactivate any existing PPL programs for this user
        await db.PplPrograms.Where(p => p.IsActive && p.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

        db.PplPrograms.Add(program);
        await db.SaveChangesAsync();

        var templates = daysPerWeek >= 6
            ? BuildSixDayTemplates(program.Id, liftDict)
            : BuildThreeDayTemplates(program.Id, liftDict);

        db.PplDayTemplates.AddRange(templates);
        await db.SaveChangesAsync();

        return program;
    }

    public async Task SetActiveProgramAsync(int programId)
    {
        var userId = await userContext.GetUserIdAsync();
        await db.PplPrograms.Where(p => p.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
        var program = await db.PplPrograms.FirstOrDefaultAsync(p => p.Id == programId && p.UserId == userId);
        if (program is not null)
        {
            program.IsActive = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task DeleteProgramAsync(int programId)
    {
        var userId = await userContext.GetUserIdAsync();
        var program = await db.PplPrograms.FirstOrDefaultAsync(p => p.Id == programId && p.UserId == userId);
        if (program is not null)
        {
            db.PplPrograms.Remove(program);
            await db.SaveChangesAsync();
        }
    }

    public async Task<PplSession> GetOrCreateNextSessionAsync(int programId)
    {
        // Check for an existing incomplete session first
        var existing = await db.PplSessions
            .Include(s => s.DayTemplate)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets)
            .Where(s => s.PplProgramId == programId && s.Status != WorkoutStatus.Completed)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing is not null) return existing;

        // Determine which day template comes next in the rotation
        var templates = await db.PplDayTemplates
            .Where(d => d.PplProgramId == programId)
            .OrderBy(d => d.OrderInWeek)
            .ToListAsync();

        if (templates.Count == 0) throw new InvalidOperationException("Program has no day templates.");

        var lastSession = await db.PplSessions
            .Where(s => s.PplProgramId == programId && s.Status == WorkoutStatus.Completed)
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefaultAsync();

        PplDayTemplate nextTemplate;
        if (lastSession is null)
        {
            nextTemplate = templates[0];
        }
        else
        {
            var lastTemplate = templates.FirstOrDefault(t => t.Id == lastSession.PplDayTemplateId);
            var lastOrder = lastTemplate?.OrderInWeek ?? 0;
            nextTemplate = templates.FirstOrDefault(t => t.OrderInWeek > lastOrder) ?? templates[0];
        }

        // Build the session with exercise snapshots
        var slots = await db.PplExerciseSlots
            .Include(s => s.Lift)
            .Where(s => s.PplDayTemplateId == nextTemplate.Id)
            .OrderBy(s => s.OrderInDay)
            .ToListAsync();

        var session = new PplSession
        {
            PplProgramId = programId,
            PplDayTemplateId = nextTemplate.Id,
            Status = WorkoutStatus.NotStarted,
            CreatedAt = DateTime.UtcNow
        };
        db.PplSessions.Add(session);
        await db.SaveChangesAsync();

        var order = 1;
        foreach (var slot in slots)
        {
            double suggestedWeight = slot.UsePercentageOfTm && slot.Lift is not null
                ? Math.Round(slot.Lift.TrainingMax * slot.TmPercentage / 5.0) * 5.0
                : slot.CurrentWeight;

            var exercise = new PplSessionExercise
            {
                PplSessionId = session.Id,
                PplExerciseSlotId = slot.Id,
                ExerciseName = slot.ExerciseName,
                TargetSets = slot.TargetSets,
                RepsMin = slot.RepsMin,
                RepsMax = slot.RepsMax,
                SuggestedWeight = suggestedWeight,
                OrderInSession = order++
            };
            db.PplSessionExercises.Add(exercise);
            await db.SaveChangesAsync();

            for (int i = 1; i <= slot.TargetSets; i++)
            {
                db.PplSessionSets.Add(new PplSessionSet
                {
                    PplSessionExerciseId = exercise.Id,
                    SetNumber = i,
                    TargetReps = slot.RepsMax
                });
            }
        }

        await db.SaveChangesAsync();
        return session;
    }

    // ── Template builders ──────────────────────────────────────────────────────

    private static List<PplDayTemplate> BuildThreeDayTemplates(int programId, Dictionary<LiftType, Lift> lifts)
    {
        var push = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Push, Variant = PplVariant.Single, OrderInWeek = 1, Name = "Push — Bench Focus" };
        push.ExerciseSlots = PushASlots(push, lifts);

        var pull = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Pull, Variant = PplVariant.Single, OrderInWeek = 2, Name = "Pull — Deadlift Focus" };
        pull.ExerciseSlots = PullASlots(pull, lifts);

        var legs = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Legs, Variant = PplVariant.Single, OrderInWeek = 3, Name = "Legs — Squat Focus" };
        legs.ExerciseSlots = LegsASlots(legs, lifts);

        return [push, pull, legs];
    }

    private static List<PplDayTemplate> BuildSixDayTemplates(int programId, Dictionary<LiftType, Lift> lifts)
    {
        var pushA = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Push, Variant = PplVariant.A, OrderInWeek = 1, Name = "Push A — Bench Focus" };
        pushA.ExerciseSlots = PushASlots(pushA, lifts);

        var pullA = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Pull, Variant = PplVariant.A, OrderInWeek = 2, Name = "Pull A — Deadlift Focus" };
        pullA.ExerciseSlots = PullASlots(pullA, lifts);

        var legsA = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Legs, Variant = PplVariant.A, OrderInWeek = 3, Name = "Legs A — Squat Focus" };
        legsA.ExerciseSlots = LegsASlots(legsA, lifts);

        var pushB = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Push, Variant = PplVariant.B, OrderInWeek = 4, Name = "Push B — OHP Focus" };
        pushB.ExerciseSlots = PushBSlots(pushB, lifts);

        var pullB = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Pull, Variant = PplVariant.B, OrderInWeek = 5, Name = "Pull B — Row Focus" };
        pullB.ExerciseSlots = PullBSlots(pullB, lifts);

        var legsB = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Legs, Variant = PplVariant.B, OrderInWeek = 6, Name = "Legs B — Hip-Hinge Focus" };
        legsB.ExerciseSlots = LegsBSlots(legsB, lifts);

        return [pushA, pullA, legsA, pushB, pullB, legsB];
    }

    // ── Exercise slot definitions ──────────────────────────────────────────────

    private static ICollection<PplExerciseSlot> PushASlots(PplDayTemplate template, Dictionary<LiftType, Lift> lifts) =>
    [
        Slot(template, 1, "Barbell Bench Press", MuscleGroup.Chest,    4, 5, 8,  pct: 0.75, lift: lifts.GetValueOrDefault(LiftType.BenchPress), inc: 5),
        Slot(template, 2, "Incline DB Press",    MuscleGroup.Chest,    3, 8, 12, inc: 2.5),
        Slot(template, 3, "Overhead Press",      MuscleGroup.Shoulders,3, 8, 10, pct: 0.60, lift: lifts.GetValueOrDefault(LiftType.OverheadPress), inc: 5),
        Slot(template, 4, "Lateral Raise",       MuscleGroup.Shoulders,4, 12,15, inc: 2.5),
        Slot(template, 5, "Tricep Pushdown",     MuscleGroup.Triceps,  3, 10,15, inc: 2.5),
        Slot(template, 6, "Overhead Tricep Ext", MuscleGroup.Triceps,  3, 10,15, inc: 2.5),
    ];

    private static ICollection<PplExerciseSlot> PushBSlots(PplDayTemplate template, Dictionary<LiftType, Lift> lifts) =>
    [
        Slot(template, 1, "Overhead Press",       MuscleGroup.Shoulders,4, 5, 8,  pct: 0.75, lift: lifts.GetValueOrDefault(LiftType.OverheadPress), inc: 5),
        Slot(template, 2, "DB Shoulder Press",    MuscleGroup.Shoulders,3, 8, 12, inc: 2.5),
        Slot(template, 3, "Incline Barbell Bench",MuscleGroup.Chest,    3, 8, 10, inc: 5),
        Slot(template, 4, "Cable Fly",            MuscleGroup.Chest,    3, 12,15, inc: 2.5),
        Slot(template, 5, "Face Pulls",           MuscleGroup.RearDelts,3, 15,20, inc: 2.5),
        Slot(template, 6, "Tricep Dips",          MuscleGroup.Triceps,  3, 8, 12, inc: 0, bodyweight: true),
    ];

    private static ICollection<PplExerciseSlot> PullASlots(PplDayTemplate template, Dictionary<LiftType, Lift> lifts) =>
    [
        Slot(template, 1, "Conventional Deadlift",MuscleGroup.Back,    3, 5, 8,  pct: 0.75, lift: lifts.GetValueOrDefault(LiftType.Deadlift), inc: 10),
        Slot(template, 2, "Lat Pulldown",         MuscleGroup.Back,    4, 8, 12, inc: 2.5),
        Slot(template, 3, "Barbell Row",          MuscleGroup.Back,    4, 8, 10, inc: 5),
        Slot(template, 4, "Cable Row",            MuscleGroup.Back,    3, 10,15, inc: 2.5),
        Slot(template, 5, "Dumbbell Curl",        MuscleGroup.Biceps,  3, 10,15, inc: 2.5),
        Slot(template, 6, "Hammer Curl",          MuscleGroup.Biceps,  3, 10,15, inc: 2.5),
    ];

    private static ICollection<PplExerciseSlot> PullBSlots(PplDayTemplate template, Dictionary<LiftType, Lift> lifts) =>
    [
        Slot(template, 1, "Barbell Row",         MuscleGroup.Back,    4, 5, 8,  inc: 5),
        Slot(template, 2, "Weighted Pull-ups",   MuscleGroup.Back,    4, 6, 10, inc: 2.5),
        Slot(template, 3, "T-Bar Row",           MuscleGroup.Back,    3, 8, 12, inc: 5),
        Slot(template, 4, "Face Pulls",          MuscleGroup.RearDelts,3,15,20, inc: 2.5),
        Slot(template, 5, "Barbell Curl",        MuscleGroup.Biceps,  3, 8, 10, inc: 5),
        Slot(template, 6, "Incline DB Curl",     MuscleGroup.Biceps,  3, 10,15, inc: 2.5),
    ];

    private static ICollection<PplExerciseSlot> LegsASlots(PplDayTemplate template, Dictionary<LiftType, Lift> lifts) =>
    [
        Slot(template, 1, "Back Squat",          MuscleGroup.Quads,      4, 5, 8,  pct: 0.75, lift: lifts.GetValueOrDefault(LiftType.Squat), inc: 10),
        Slot(template, 2, "Romanian Deadlift",   MuscleGroup.Hamstrings, 3, 8, 12, inc: 5),
        Slot(template, 3, "Leg Press",           MuscleGroup.Quads,      3, 10,15, inc: 10),
        Slot(template, 4, "Leg Curl",            MuscleGroup.Hamstrings, 3, 10,15, inc: 2.5),
        Slot(template, 5, "Standing Calf Raise", MuscleGroup.Calves,     4, 12,20, inc: 5),
    ];

    private static ICollection<PplExerciseSlot> LegsBSlots(PplDayTemplate template, Dictionary<LiftType, Lift> lifts) =>
    [
        Slot(template, 1, "Romanian Deadlift",      MuscleGroup.Hamstrings, 4, 6, 10, inc: 5),
        Slot(template, 2, "Hack Squat",             MuscleGroup.Quads,      3, 8, 12, inc: 10),
        Slot(template, 3, "Bulgarian Split Squat",  MuscleGroup.Glutes,     3, 8, 12, inc: 2.5),
        Slot(template, 4, "Leg Extension",          MuscleGroup.Quads,      3, 10,15, inc: 2.5),
        Slot(template, 5, "Seated Leg Curl",        MuscleGroup.Hamstrings, 3, 10,15, inc: 2.5),
        Slot(template, 6, "Seated Calf Raise",      MuscleGroup.Calves,     4, 15,20, inc: 2.5),
    ];

    private static PplExerciseSlot Slot(
        PplDayTemplate template, int order, string name, MuscleGroup muscle,
        int sets, int repsMin, int repsMax,
        double? pct = null, Lift? lift = null, double inc = 5, bool bodyweight = false) =>
        new()
        {
            PplDayTemplateId = template.Id,
            OrderInDay = order,
            ExerciseName = name,
            MuscleGroup = muscle,
            TargetSets = sets,
            RepsMin = repsMin,
            RepsMax = repsMax,
            UsePercentageOfTm = pct.HasValue,
            TmPercentage = pct ?? 0,
            LiftId = lift?.Id,
            Lift = lift,
            CurrentWeight = 0,
            ProgressionIncrement = inc,
            IsBodyweight = bodyweight
        };
}
