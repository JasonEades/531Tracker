using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IPplProgramService
{
    Task<PplProgram?> GetActiveProgramAsync();
    Task<List<PplProgram>> GetAllProgramsAsync();
    Task<PplProgram?> GetProgramWithDetailsAsync(int programId);
    Task<List<PplDayTemplate>> GetWeekWorkoutTemplatesAsync(int programId, int weekNumber);
    Task ConfigureWeekWorkoutsAsync(int programId, int weekNumber, IReadOnlyList<PplWorkoutDayDraft> days);
    Task<PplProgram> CreateProgramAsync(string name, int daysPerWeek, string? notes = null);
    Task ConfigureStartingWeightsAsync(int programId, IReadOnlyDictionary<int, double?> weights);
    Task SetActiveProgramAsync(int programId);
    Task DeleteProgramAsync(int programId);
    Task<PplSession> GetOrCreateNextSessionAsync(int programId);
    Task<PplWeekPlan> GetWeekPlanAsync(int programId);
    Task<PplSession> StartSessionAsync(int programId, int dayTemplateId);
    Task<int> GetCurrentWeekNumberAsync(int programId);
    Task<PplProgramOverview?> GetProgramOverviewAsync(int programId, int upcomingWeekCount = 3);
}

public class PplProgramService(
    AppDbContext db,
    ICurrentUserService userContext,
    IEstimatedStrengthService estimatedStrength,
    IWeightCalculator weightCalculator) : IPplProgramService
{
    public async Task<PplProgram?> GetActiveProgramAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.PplPrograms.Include(p => p.Weeks).FirstOrDefaultAsync(p => p.IsActive && p.UserId == userId);
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

    public async Task<List<PplDayTemplate>> GetWeekWorkoutTemplatesAsync(int programId, int weekNumber)
    {
        if (weekNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(weekNumber));

        var userId = await userContext.GetUserIdAsync();
        var hasProgram = await db.PplPrograms.AnyAsync(p => p.Id == programId && p.UserId == userId);
        if (!hasProgram) throw new InvalidOperationException("PPL program was not found.");

        var templates = await db.PplDayTemplates
            .Include(d => d.ExerciseSlots.OrderBy(s => s.OrderInDay))
            .Where(d => d.PplProgramId == programId && d.WeekNumber == weekNumber)
            .OrderBy(d => d.OrderInWeek)
            .ToListAsync();
        if (templates.Count > 0) return templates;

        return await db.PplDayTemplates
            .Include(d => d.ExerciseSlots.OrderBy(s => s.OrderInDay))
            .Where(d => d.PplProgramId == programId && d.WeekNumber == null)
            .OrderBy(d => d.OrderInWeek)
            .ToListAsync();
    }

    public async Task ConfigureWeekWorkoutsAsync(
        int programId,
        int weekNumber,
        IReadOnlyList<PplWorkoutDayDraft> days)
    {
        if (weekNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(weekNumber));

        var userId = await userContext.GetUserIdAsync();
        var program = await db.PplPrograms.FirstOrDefaultAsync(p => p.Id == programId && p.UserId == userId)
            ?? throw new InvalidOperationException("PPL program was not found.");

        var defaults = await db.PplDayTemplates
            .Include(d => d.ExerciseSlots)
            .Where(d => d.PplProgramId == programId && d.WeekNumber == null)
            .OrderBy(d => d.OrderInWeek)
            .ToListAsync();
        if (days.Count != defaults.Count || days.Select(d => d.DayTemplateId).Distinct().Count() != defaults.Count
            || days.Any(d => defaults.All(t => t.Id != d.DayTemplateId)))
            throw new InvalidOperationException("Workout days do not match this program's schedule.");
        if (days.Any(d => string.IsNullOrWhiteSpace(d.Name) || d.Name.Length > 100
            || d.Exercises.Count == 0
            || d.Exercises.Any(e => string.IsNullOrWhiteSpace(e.ExerciseName) || e.ExerciseName.Length > 100
                || e.TargetSets is < 1 or > 20 || e.RepsMin is < 1 or > 100
                || e.RepsMax < e.RepsMin || e.RepsMax > 100
                || e.ProgressionIncrement is < 0 or > 100)))
            throw new InvalidOperationException("Enter a valid name and at least one exercise with valid sets and reps for every workout.");

        var selectedExerciseIds = days.SelectMany(day => day.Exercises)
            .Where(exercise => exercise.ExerciseDefinitionId.HasValue)
            .Select(exercise => exercise.ExerciseDefinitionId!.Value)
            .Distinct()
            .ToList();
        var selectedDefinitions = selectedExerciseIds.Count == 0
            ? []
            : await db.ExerciseDefinitions
                .Where(definition => selectedExerciseIds.Contains(definition.Id)
                    && (definition.UserId == null || definition.UserId == userId))
                .ToDictionaryAsync(definition => definition.Id);
        if (selectedDefinitions.Count != selectedExerciseIds.Count)
            throw new InvalidOperationException("One or more selected exercises are unavailable.");
        if (days.SelectMany(day => day.Exercises).Any(exercise =>
            exercise.ExerciseDefinitionId is int definitionId
            && (!string.Equals(selectedDefinitions[definitionId].Name, exercise.ExerciseName.Trim(), StringComparison.Ordinal)
                || selectedDefinitions[definitionId].PrimaryMuscle != exercise.MuscleGroup)))
            throw new InvalidOperationException("The selected exercise details have changed. Reload the workout configuration and try again.");

        var profile = await estimatedStrength.GetProfileAsync(userId);

        var sessions = await db.PplSessions
            .Include(s => s.Week)
            .Where(s => s.PplProgramId == programId && s.Week != null && s.Week.WeekNumber == weekNumber)
            .ToListAsync();
        if (sessions.Any(s => s.Status != WorkoutStatus.NotStarted))
            throw new InvalidOperationException("This week's workouts cannot be changed after a session has started.");

        var selectedTemplates = await GetWeekWorkoutTemplatesAsync(programId, weekNumber);
        var allowedSourceSlotIds = selectedTemplates.SelectMany(t => t.ExerciseSlots).Select(s => s.Id).ToHashSet();
        if (days.SelectMany(d => d.Exercises).Any(e => e.SourceSlotId is int id && !allowedSourceSlotIds.Contains(id)))
            throw new InvalidOperationException("An exercise no longer belongs to this week's workout configuration.");

        db.PplSessions.RemoveRange(sessions);
        await db.SaveChangesAsync();

        var existingWeekTemplates = await db.PplDayTemplates
            .Include(d => d.ExerciseSlots)
            .Where(d => d.PplProgramId == programId && d.WeekNumber == weekNumber)
            .ToListAsync();
        db.PplDayTemplates.RemoveRange(existingWeekTemplates);
        await db.SaveChangesAsync();

        var sourceSlots = selectedTemplates.SelectMany(t => t.ExerciseSlots).ToDictionary(s => s.Id);
        foreach (var draft in days)
        {
            var defaultTemplate = defaults.Single(t => t.Id == draft.DayTemplateId);
            var template = new PplDayTemplate
            {
                PplProgramId = program.Id,
                DayType = defaultTemplate.DayType,
                Variant = defaultTemplate.Variant,
                OrderInWeek = defaultTemplate.OrderInWeek,
                WeekNumber = weekNumber,
                Name = draft.Name.Trim()
            };

            for (var index = 0; index < draft.Exercises.Count; index++)
            {
                var exercise = draft.Exercises[index];
                var source = exercise.SourceSlotId is int sourceId ? sourceSlots[sourceId] : null;
                var definition = exercise.ExerciseDefinitionId is int definitionId
                    ? selectedDefinitions[definitionId]
                    : null;
                var keepProgress = source is not null
                    && source.MuscleGroup == exercise.MuscleGroup
                    && source.ExerciseDefinitionId == exercise.ExerciseDefinitionId
                    && string.Equals(source.ExerciseName, exercise.ExerciseName.Trim(), StringComparison.OrdinalIgnoreCase);
                var oneRepMax = keepProgress ? null : profile.GetOneRepMax(exercise.ExerciseName.Trim());
                var derivedWeight = oneRepMax is > 0
                    ? weightCalculator.RoundToNearest5(oneRepMax.Value / (1 + exercise.RepsMax / 30.0))
                    : (double?)null;
                template.ExerciseSlots.Add(new PplExerciseSlot
                {
                    OrderInDay = index + 1,
                    ExerciseName = exercise.ExerciseName.Trim(),
                    ExerciseDefinitionId = exercise.ExerciseDefinitionId,
                    MuscleGroup = exercise.MuscleGroup,
                    TargetSets = exercise.TargetSets,
                    RepsMin = exercise.RepsMin,
                    RepsMax = exercise.RepsMax,
                    IsBodyweight = exercise.IsBodyweight,
                    ProgressionIncrement = exercise.ProgressionIncrement,
                    StartingWeight = keepProgress ? source!.StartingWeight : derivedWeight,
                    CurrentWeight = keepProgress ? source!.CurrentWeight : derivedWeight,
                    BarId = keepProgress ? source!.BarId : null
                });
            }

            db.PplDayTemplates.Add(template);
        }

        await db.SaveChangesAsync();
    }

    public async Task<PplProgram> CreateProgramAsync(string name, int daysPerWeek, string? notes = null)
    {
        var userId = await userContext.GetUserIdAsync();
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
            ? BuildSixDayTemplates(program.Id)
            : BuildThreeDayTemplates(program.Id);

        var catalogByName = await db.ExerciseDefinitions
            .Where(definition => definition.UserId == null)
            .ToDictionaryAsync(definition => definition.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var slot in templates.SelectMany(template => template.ExerciseSlots))
        {
            if (catalogByName.TryGetValue(slot.ExerciseName, out var definition))
                slot.ExerciseDefinitionId = definition.Id;
        }

        var profile = await estimatedStrength.GetProfileAsync(userId);
        foreach (var slot in templates.SelectMany(template => template.ExerciseSlots))
        {
            if (slot.IsBodyweight) continue;
            var oneRepMax = profile.GetOneRepMax(slot.ExerciseName);
            if (oneRepMax is not > 0) continue;

            var startingWeight = weightCalculator.RoundToNearest5(oneRepMax.Value / (1 + slot.RepsMax / 30.0));
            slot.StartingWeight = startingWeight;
            slot.CurrentWeight = startingWeight;
        }

        db.PplDayTemplates.AddRange(templates);
        await db.SaveChangesAsync();

        return program;
    }

    public async Task ConfigureStartingWeightsAsync(int programId, IReadOnlyDictionary<int, double?> weights)
    {
        var userId = await userContext.GetUserIdAsync();
        var slots = await db.PplExerciseSlots
            .Include(s => s.DayTemplate)
            .Where(s => s.DayTemplate.PplProgramId == programId && s.DayTemplate.Program.UserId == userId)
            .ToListAsync();

        foreach (var slot in slots)
        {
            if (!weights.TryGetValue(slot.Id, out var weight)) continue;
            var normalized = weight is > 0 ? weight : null;
            slot.StartingWeight = normalized;
            slot.CurrentWeight = normalized;
        }

        await db.SaveChangesAsync();
    }

    public async Task SetActiveProgramAsync(int programId)
    {
        var userId = await userContext.GetUserIdAsync();
        var program = await db.PplPrograms.FirstOrDefaultAsync(p => p.Id == programId && p.UserId == userId);
        if (program is null) return;

        await db.PplPrograms.Where(p => p.UserId == userId).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));
        program.IsActive = true;
        await db.SaveChangesAsync();
    }

    public async Task DeleteProgramAsync(int programId)
    {
        var userId = await userContext.GetUserIdAsync();
        var program = await db.PplPrograms.FirstOrDefaultAsync(p => p.Id == programId && p.UserId == userId);
        if (program is not null)
        {
            // Sessions reference day templates/slots with RESTRICT FKs; PostgreSQL checks those during
            // the program cascade, so remove sessions before the program.
            var sessions = await db.PplSessions.Where(s => s.PplProgramId == program.Id).ToListAsync();
            db.PplSessions.RemoveRange(sessions);
            await db.SaveChangesAsync();

            db.PplPrograms.Remove(program);
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> GetCurrentWeekNumberAsync(int programId)
    {
        var userId = await userContext.GetUserIdAsync();
        if (!await db.PplPrograms.AnyAsync(p => p.Id == programId && p.UserId == userId))
            throw new InvalidOperationException("PPL program was not found.");

        var completedCount = await db.PplSessions
            .CountAsync(s => s.PplProgramId == programId && s.Status == WorkoutStatus.Completed);
        return await GetCurrentWeekNumberCoreAsync(programId, completedCount);
    }

    private async Task<int> GetCurrentWeekNumberCoreAsync(int programId, int completedCount)
    {
        var daysPerRotation = await db.PplDayTemplates.CountAsync(d => d.PplProgramId == programId && d.WeekNumber == null);
        return daysPerRotation == 0 ? 1 : completedCount / daysPerRotation + 1;
    }

    public async Task<PplSession> GetOrCreateNextSessionAsync(int programId)
    {
        var userId = await userContext.GetUserIdAsync();
        await EnsureProgramOwnedAsync(programId, userId);

        var open = await LoadOpenSessionAsync(programId, userId);
        if (open is not null) return open;

        var week = await LoadWeekContextAsync(programId);
        var nextTemplate = week.Templates.FirstOrDefault(t => !week.CompletedOrders.Contains(t.OrderInWeek))
            ?? week.Templates[week.CompletedCount % week.Templates.Count];
        return await CreateSessionAsync(programId, userId, nextTemplate, week.WeekNumber);
    }

    public async Task<PplWeekPlan> GetWeekPlanAsync(int programId)
    {
        var userId = await userContext.GetUserIdAsync();
        await EnsureProgramOwnedAsync(programId, userId);

        var week = await LoadWeekContextAsync(programId);
        var openSessions = await db.PplSessions.AsNoTracking()
            .Where(s => s.PplProgramId == programId && s.Status != WorkoutStatus.Completed)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.DayTemplate.OrderInWeek,
                HasLoggedSets = s.Exercises.SelectMany(e => e.Sets).Any(set => set.IsCompleted || set.ActualWeight != null || set.ActualReps != null)
            })
            .ToListAsync();
        var open = openSessions.FirstOrDefault();

        var days = week.Templates
            .Select(t =>
            {
                if (open is not null && open.OrderInWeek == t.OrderInWeek)
                    return new PplWeekDay(t.Id, t.OrderInWeek, t.Name, t.DayType, PplDayState.Open, open.Id, open.HasLoggedSets);
                var completedId = week.CompletedSessionIds.GetValueOrDefault(t.OrderInWeek);
                return week.CompletedOrders.Contains(t.OrderInWeek)
                    ? new PplWeekDay(t.Id, t.OrderInWeek, t.Name, t.DayType, PplDayState.Completed, completedId, true)
                    : new PplWeekDay(t.Id, t.OrderInWeek, t.Name, t.DayType, PplDayState.Available, null, false);
            })
            .ToList();

        var suggested = days.FirstOrDefault(d => d.State == PplDayState.Open)
            ?? days.FirstOrDefault(d => d.State == PplDayState.Available);
        return new PplWeekPlan(week.WeekNumber, days, suggested?.DayTemplateId);
    }

    public async Task<PplSession> StartSessionAsync(int programId, int dayTemplateId)
    {
        var userId = await userContext.GetUserIdAsync();
        await EnsureProgramOwnedAsync(programId, userId);

        var week = await LoadWeekContextAsync(programId);
        var template = week.Templates.FirstOrDefault(t => t.Id == dayTemplateId)
            ?? throw new InvalidOperationException("That workout isn't part of this week.");
        if (week.CompletedOrders.Contains(template.OrderInWeek))
            throw new InvalidOperationException($"{template.Name} is already done this week.");

        var open = await LoadOpenSessionAsync(programId, userId);
        if (open is not null)
        {
            if (open.DayTemplate.OrderInWeek == template.OrderInWeek)
                return open;

            var hasLoggedSets = open.Exercises.SelectMany(e => e.Sets)
                .Any(set => set.IsCompleted || set.ActualWeight != null || set.ActualReps != null);
            if (hasLoggedSets)
                throw new InvalidOperationException($"Finish or delete your in-progress {open.DayTemplate.Name} session first.");

            // Nothing logged yet, so switching days just replaces the untouched session.
            db.PplSessions.Remove(open);
            await db.SaveChangesAsync();
        }

        return await CreateSessionAsync(programId, userId, template, week.WeekNumber);
    }

    public async Task<PplProgramOverview?> GetProgramOverviewAsync(int programId, int upcomingWeekCount = 3)
    {
        var userId = await userContext.GetUserIdAsync();
        var program = await db.PplPrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == programId && p.UserId == userId);
        if (program is null) return null;

        var plan = await GetWeekPlanAsync(programId);

        var templates = await db.PplDayTemplates.AsNoTracking()
            .Include(d => d.ExerciseSlots.OrderBy(s => s.OrderInDay))
            .Where(d => d.PplProgramId == programId)
            .OrderBy(d => d.OrderInWeek)
            .ToListAsync();
        PplScheduledWeek Schedule(int weekNumber)
        {
            var custom = templates.Where(t => t.WeekNumber == weekNumber).ToList();
            return custom.Count > 0
                ? new PplScheduledWeek(weekNumber, true, custom)
                : new PplScheduledWeek(weekNumber, false, templates.Where(t => t.WeekNumber is null).ToList());
        }

        var weeks = await db.PplWeeks.AsNoTracking()
            .Include(w => w.Sessions).ThenInclude(s => s.DayTemplate)
            .Include(w => w.Sessions).ThenInclude(s => s.Exercises).ThenInclude(e => e.Sets)
            .Include(w => w.AdditionalSessions).ThenInclude(s => s.CardioEntries).ThenInclude(e => e.Accessory)
            .Include(w => w.AdditionalSessions).ThenInclude(s => s.StrengthExercises)
            .Where(w => w.PplProgramId == programId)
            .OrderByDescending(w => w.WeekNumber)
            .AsSplitQuery()
            .ToListAsync();

        var unassigned = await db.PplSessions.AsNoTracking()
            .Include(s => s.DayTemplate)
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .Where(s => s.PplProgramId == programId && s.PplWeekId == null)
            .OrderByDescending(s => s.OccurredOn)
            .AsSplitQuery()
            .ToListAsync();

        return new PplProgramOverview(
            program,
            plan,
            Schedule(plan.WeekNumber),
            Enumerable.Range(plan.WeekNumber + 1, Math.Max(0, upcomingWeekCount)).Select(Schedule).ToList(),
            weeks,
            unassigned);
    }

    private async Task EnsureProgramOwnedAsync(int programId, string userId)
    {
        if (!await db.PplPrograms.AnyAsync(p => p.Id == programId && p.UserId == userId))
            throw new InvalidOperationException("PPL program was not found.");
    }

    private Task<PplSession?> LoadOpenSessionAsync(int programId, string userId) =>
        db.PplSessions
            .Include(s => s.DayTemplate)
            .Include(s => s.Exercises)
                .ThenInclude(e => e.Sets)
            .Where(s => s.PplProgramId == programId && s.Program.UserId == userId && s.Status != WorkoutStatus.Completed)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

    private sealed record WeekContext(
        int WeekNumber,
        int CompletedCount,
        List<PplDayTemplate> Templates,
        HashSet<int> CompletedOrders,
        Dictionary<int, int> CompletedSessionIds);

    private async Task<WeekContext> LoadWeekContextAsync(int programId)
    {
        var completedCount = await db.PplSessions
            .CountAsync(s => s.PplProgramId == programId && s.Status == WorkoutStatus.Completed);
        var weekNumber = await GetCurrentWeekNumberCoreAsync(programId, completedCount);

        // Select this week's customized schedule when one exists, otherwise use the program defaults.
        var availableTemplates = await db.PplDayTemplates
            .Where(d => d.PplProgramId == programId && (d.WeekNumber == weekNumber || d.WeekNumber == null))
            .OrderBy(d => d.OrderInWeek)
            .ToListAsync();
        var weekTemplates = availableTemplates.Where(d => d.WeekNumber == weekNumber).ToList();
        var templates = weekTemplates.Count > 0
            ? weekTemplates
            : availableTemplates.Where(d => d.WeekNumber is null).ToList();
        if (templates.Count == 0) throw new InvalidOperationException("Program has no day templates.");

        // Weeks advance by completed rotations, not calendar time; days within a week can be done in any order.
        // Days are matched by slot order so they survive week-specific template customization.
        var completedThisWeek = await db.PplSessions.AsNoTracking()
            .Where(s => s.PplProgramId == programId && s.Status == WorkoutStatus.Completed
                && s.Week != null && s.Week.WeekNumber == weekNumber)
            .OrderByDescending(s => s.CompletedAt)
            .Select(s => new { s.Id, s.DayTemplate.OrderInWeek })
            .ToListAsync();

        return new WeekContext(
            weekNumber,
            completedCount,
            templates,
            completedThisWeek.Select(s => s.OrderInWeek).ToHashSet(),
            completedThisWeek.GroupBy(s => s.OrderInWeek).ToDictionary(g => g.Key, g => g.First().Id));
    }

    private async Task<PplSession> CreateSessionAsync(int programId, string userId, PplDayTemplate nextTemplate, int weekNumber)
    {
        var sessionDate = DateTime.UtcNow.Date;
        // Build the session with exercise snapshots
        var slots = await db.PplExerciseSlots
            .Include(s => s.Lift)
            .Where(s => s.PplDayTemplateId == nextTemplate.Id && s.DayTemplate.Program.UserId == userId)
            .OrderBy(s => s.OrderInDay)
            .ToListAsync();

        var session = new PplSession
        {
            PplProgramId = programId,
            PplDayTemplateId = nextTemplate.Id,
            DayTemplate = nextTemplate,
            Status = WorkoutStatus.NotStarted,
            CreatedAt = DateTime.UtcNow,
            OccurredOn = sessionDate
        };
        db.PplSessions.Add(session);
        await db.SaveChangesAsync();

        var pplWeek = await db.PplWeeks.FirstOrDefaultAsync(w => w.PplProgramId == programId && w.WeekNumber == weekNumber);
        if (pplWeek is null)
        {
            pplWeek = new PplWeek { PplProgramId = programId, WeekNumber = weekNumber, StartDate = sessionDate };
            db.PplWeeks.Add(pplWeek);
            await db.SaveChangesAsync();
        }
        session.PplWeekId = pplWeek.Id;
        session.Week = pplWeek;
        await db.SaveChangesAsync();

        var order = 1;
        foreach (var slot in slots)
        {
            var exercise = new PplSessionExercise
            {
                PplSessionId = session.Id,
                PplExerciseSlotId = slot.Id,
                ExerciseName = slot.ExerciseName,
                TargetSets = slot.TargetSets,
                RepsMin = slot.RepsMin,
                RepsMax = slot.RepsMax,
                SuggestedWeight = slot.CurrentWeight,
                BarId = slot.BarId,
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

    private static List<PplDayTemplate> BuildThreeDayTemplates(int programId)
    {
        var push = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Push, Variant = PplVariant.Single, OrderInWeek = 1, Name = "Push — Bench Focus" };
        push.ExerciseSlots = PushASlots(push);

        var pull = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Pull, Variant = PplVariant.Single, OrderInWeek = 2, Name = "Pull — Deadlift Focus" };
        pull.ExerciseSlots = PullASlots(pull);

        var legs = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Legs, Variant = PplVariant.Single, OrderInWeek = 3, Name = "Legs — Squat Focus" };
        legs.ExerciseSlots = LegsASlots(legs);

        return [push, pull, legs];
    }

    private static List<PplDayTemplate> BuildSixDayTemplates(int programId)
    {
        var pushA = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Push, Variant = PplVariant.A, OrderInWeek = 1, Name = "Push A — Bench Focus" };
        pushA.ExerciseSlots = PushASlots(pushA);

        var pullA = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Pull, Variant = PplVariant.A, OrderInWeek = 2, Name = "Pull A — Deadlift Focus" };
        pullA.ExerciseSlots = PullASlots(pullA);

        var legsA = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Legs, Variant = PplVariant.A, OrderInWeek = 3, Name = "Legs A — Squat Focus" };
        legsA.ExerciseSlots = LegsASlots(legsA);

        var pushB = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Push, Variant = PplVariant.B, OrderInWeek = 4, Name = "Push B — OHP Focus" };
        pushB.ExerciseSlots = PushBSlots(pushB);

        var pullB = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Pull, Variant = PplVariant.B, OrderInWeek = 5, Name = "Pull B — Row Focus" };
        pullB.ExerciseSlots = PullBSlots(pullB);

        var legsB = new PplDayTemplate { PplProgramId = programId, DayType = PplDayType.Legs, Variant = PplVariant.B, OrderInWeek = 6, Name = "Legs B — Hip-Hinge Focus" };
        legsB.ExerciseSlots = LegsBSlots(legsB);

        return [pushA, pullA, legsA, pushB, pullB, legsB];
    }

    // ── Exercise slot definitions ──────────────────────────────────────────────

    private static ICollection<PplExerciseSlot> PushASlots(PplDayTemplate template) =>
    [
        Slot(template, 1, "Barbell Bench Press", MuscleGroup.Chest,    4, 5, 8, inc: 5),
        Slot(template, 2, "Incline DB Press",    MuscleGroup.Chest,    3, 8, 12, inc: 2.5),
        Slot(template, 3, "Overhead Press",      MuscleGroup.Shoulders,3, 8, 10, inc: 5),
        Slot(template, 4, "Lateral Raise",       MuscleGroup.Shoulders,4, 12,15, inc: 2.5),
        Slot(template, 5, "Tricep Pushdown",     MuscleGroup.Triceps,  3, 10,15, inc: 2.5),
        Slot(template, 6, "Overhead Tricep Ext", MuscleGroup.Triceps,  3, 10,15, inc: 2.5),
    ];

    private static ICollection<PplExerciseSlot> PushBSlots(PplDayTemplate template) =>
    [
        Slot(template, 1, "Overhead Press",       MuscleGroup.Shoulders,4, 5, 8, inc: 5),
        Slot(template, 2, "DB Shoulder Press",    MuscleGroup.Shoulders,3, 8, 12, inc: 2.5),
        Slot(template, 3, "Incline Barbell Bench",MuscleGroup.Chest,    3, 8, 10, inc: 5),
        Slot(template, 4, "Cable Fly",            MuscleGroup.Chest,    3, 12,15, inc: 2.5),
        Slot(template, 5, "Face Pulls",           MuscleGroup.RearDelts,3, 15,20, inc: 2.5),
        Slot(template, 6, "Tricep Dips",          MuscleGroup.Triceps,  3, 8, 12, inc: 0, bodyweight: true),
    ];

    private static ICollection<PplExerciseSlot> PullASlots(PplDayTemplate template) =>
    [
        Slot(template, 1, "Conventional Deadlift",MuscleGroup.Back,    3, 5, 8, inc: 10),
        Slot(template, 2, "Lat Pulldown",         MuscleGroup.Back,    4, 8, 12, inc: 2.5),
        Slot(template, 3, "Barbell Row",          MuscleGroup.Back,    4, 8, 10, inc: 5),
        Slot(template, 4, "Cable Row",            MuscleGroup.Back,    3, 10,15, inc: 2.5),
        Slot(template, 5, "Dumbbell Curl",        MuscleGroup.Biceps,  3, 10,15, inc: 2.5),
        Slot(template, 6, "Hammer Curl",          MuscleGroup.Biceps,  3, 10,15, inc: 2.5),
    ];

    private static ICollection<PplExerciseSlot> PullBSlots(PplDayTemplate template) =>
    [
        Slot(template, 1, "Barbell Row",         MuscleGroup.Back,    4, 5, 8,  inc: 5),
        Slot(template, 2, "Weighted Pull-ups",   MuscleGroup.Back,    4, 6, 10, inc: 2.5),
        Slot(template, 3, "T-Bar Row",           MuscleGroup.Back,    3, 8, 12, inc: 5),
        Slot(template, 4, "Face Pulls",          MuscleGroup.RearDelts,3,15,20, inc: 2.5),
        Slot(template, 5, "Barbell Curl",        MuscleGroup.Biceps,  3, 8, 10, inc: 5),
        Slot(template, 6, "Incline DB Curl",     MuscleGroup.Biceps,  3, 10,15, inc: 2.5),
    ];

    private static ICollection<PplExerciseSlot> LegsASlots(PplDayTemplate template) =>
    [
        Slot(template, 1, "Back Squat",          MuscleGroup.Quads,      4, 5, 8, inc: 10),
        Slot(template, 2, "Romanian Deadlift",   MuscleGroup.Hamstrings, 3, 8, 12, inc: 5),
        Slot(template, 3, "Leg Press",           MuscleGroup.Quads,      3, 10,15, inc: 10),
        Slot(template, 4, "Leg Curl",            MuscleGroup.Hamstrings, 3, 10,15, inc: 2.5),
        Slot(template, 5, "Standing Calf Raise", MuscleGroup.Calves,     4, 12,20, inc: 5),
    ];

    private static ICollection<PplExerciseSlot> LegsBSlots(PplDayTemplate template) =>
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
        double inc = 5, bool bodyweight = false) =>
        new()
        {
            PplDayTemplateId = template.Id,
            OrderInDay = order,
            ExerciseName = name,
            MuscleGroup = muscle,
            TargetSets = sets,
            RepsMin = repsMin,
            RepsMax = repsMax,
            UsePercentageOfTm = false,
            TmPercentage = 0,
            CurrentWeight = null,
            ProgressionIncrement = inc,
            IsBodyweight = bodyweight
        };
}
