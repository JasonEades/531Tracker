using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IAdditionalSessionService
{
    Task<List<Accessory>> GetCardioAccessoriesAsync();
    Task<AdditionalSession?> GetAsync(int id);
    Task<AdditionalSession> CreateForWeekAsync(int weekId, SessionType type, DateTime occurredOn, string? name, string? notes);
    Task<AdditionalSession> CreateForCycleAsync(int cycleId, SessionType type, DateTime occurredOn, string? name, string? notes);
    Task<AdditionalSession> CreateForPplWeekAsync(int pplWeekId, SessionType type, DateTime occurredOn, string? name, string? notes);
    Task UpdateAsync(int id, DateTime occurredOn, string? name, string? notes, IReadOnlyCollection<CardioEntryInput>? cardioEntries = null);
    Task DeleteAsync(int id);
    Task<AdditionalStrengthExercise?> AddStrengthExerciseAsync(int sessionId, string exerciseName);
    Task<AdditionalStrengthSet?> AddStrengthSetAsync(int exerciseId, double? weight, int? reps, string? notes);
    Task<CardioEntry?> AddCardioEntryAsync(int sessionId, int accessoryId, double quantity, CardioUnit unit, string? notes);
}

public sealed record CardioEntryInput(int AccessoryId, double Quantity, CardioUnit Unit, string? Notes);

public sealed class AdditionalSessionService(AppDbContext db, ICurrentUserService userContext) : IAdditionalSessionService
{
    public async Task<List<Accessory>> GetCardioAccessoriesAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.Accessories
            .Where(a => a.IsActive && a.Category == AccessoryCategory.Cardio && (a.UserId == null || a.UserId == userId))
            .OrderBy(a => a.Name)
            .ToListAsync();
    }
    public async Task<AdditionalSession?> GetAsync(int id)
        => await OwnedQuery(await userContext.GetUserIdAsync())
            .Include(s => s.StrengthExercises).ThenInclude(e => e.Sets)
            .Include(s => s.CardioEntries).ThenInclude(e => e.Accessory)
            .SingleOrDefaultAsync(s => s.Id == id);

    public Task<AdditionalSession> CreateForWeekAsync(int weekId, SessionType type, DateTime occurredOn, string? name, string? notes)
        => CreateAsync(new AdditionalSession { WeekId = weekId }, type, occurredOn, name, notes);

    public Task<AdditionalSession> CreateForCycleAsync(int cycleId, SessionType type, DateTime occurredOn, string? name, string? notes)
        => CreateAsync(new AdditionalSession { CycleId = cycleId }, type, occurredOn, name, notes);

    public Task<AdditionalSession> CreateForPplWeekAsync(int pplWeekId, SessionType type, DateTime occurredOn, string? name, string? notes)
        => CreateAsync(new AdditionalSession { PplWeekId = pplWeekId }, type, occurredOn, name, notes);

    public async Task UpdateAsync(int id, DateTime occurredOn, string? name, string? notes, IReadOnlyCollection<CardioEntryInput>? cardioEntries = null)
    {
        var session = await OwnedQuery(await userContext.GetUserIdAsync())
            .Include(s => s.CardioEntries)
            .SingleOrDefaultAsync(s => s.Id == id);
        if (session is null) return;
        session.OccurredOn = DateTime.SpecifyKind(occurredOn.Date, DateTimeKind.Utc);
        session.Name = string.IsNullOrWhiteSpace(name) ? session.SessionType switch
        {
            SessionType.Cardio => "Cardio Session",
            SessionType.CustomStrength => "Custom Workout",
            _ => "Additional Session"
        } : name.Trim();
        session.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (session.SessionType == SessionType.Cardio && cardioEntries is not null)
        {
            db.CardioEntries.RemoveRange(session.CardioEntries);
            foreach (var entry in cardioEntries.Where(e => e.AccessoryId > 0 && e.Quantity > 0))
            {
                db.CardioEntries.Add(new CardioEntry
                {
                    AdditionalSessionId = session.Id,
                    AccessoryId = entry.AccessoryId,
                    Quantity = entry.Quantity,
                    Unit = entry.Unit,
                    Notes = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes.Trim()
                });
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var session = await OwnedQuery(await userContext.GetUserIdAsync()).SingleOrDefaultAsync(s => s.Id == id);
        if (session is null) return;
        db.AdditionalSessions.Remove(session);
        await db.SaveChangesAsync();
    }

    public async Task<AdditionalStrengthExercise?> AddStrengthExerciseAsync(int sessionId, string exerciseName)
    {
        if (string.IsNullOrWhiteSpace(exerciseName)) return null;
        var session = await OwnedQuery(await userContext.GetUserIdAsync()).Include(s => s.StrengthExercises).SingleOrDefaultAsync(s => s.Id == sessionId);
        if (session is null || session.SessionType != SessionType.CustomStrength) return null;
        var exercise = new AdditionalStrengthExercise { AdditionalSessionId = sessionId, ExerciseName = exerciseName.Trim(), Order = session.StrengthExercises.Count + 1 };
        db.AdditionalStrengthExercises.Add(exercise);
        await db.SaveChangesAsync();
        return exercise;
    }

    public async Task<AdditionalStrengthSet?> AddStrengthSetAsync(int exerciseId, double? weight, int? reps, string? notes)
    {
        if (weight is <= 0 || reps is <= 0) return null;
        var userId = await userContext.GetUserIdAsync();
        var exercise = await db.AdditionalStrengthExercises
            .Include(e => e.Session).ThenInclude(s => s.Week).ThenInclude(w => w!.Cycle)
            .Include(e => e.Session).ThenInclude(s => s.PplWeek).ThenInclude(w => w!.Program)
            .SingleOrDefaultAsync(e => e.Id == exerciseId &&
                ((e.Session.WeekId != null && e.Session.Week!.Cycle.UserId == userId) ||
                 (e.Session.PplWeekId != null && e.Session.PplWeek!.Program.UserId == userId)));
        if (exercise is null) return null;
        var set = new AdditionalStrengthSet { AdditionalStrengthExerciseId = exerciseId, SetNumber = await db.AdditionalStrengthSets.CountAsync(s => s.AdditionalStrengthExerciseId == exerciseId) + 1, Weight = weight, Reps = reps, Notes = notes };
        db.AdditionalStrengthSets.Add(set);
        await db.SaveChangesAsync();
        return set;
    }

    public async Task<CardioEntry?> AddCardioEntryAsync(int sessionId, int accessoryId, double quantity, CardioUnit unit, string? notes)
    {
        if (quantity <= 0 || !Enum.IsDefined(unit)) return null;
        var session = await OwnedQuery(await userContext.GetUserIdAsync()).SingleOrDefaultAsync(s => s.Id == sessionId);
        var accessory = await db.Accessories.SingleOrDefaultAsync(a => a.Id == accessoryId && a.IsActive && a.Category == AccessoryCategory.Cardio);
        if (session is null || session.SessionType != SessionType.Cardio || accessory is null) return null;
        var entry = new CardioEntry { AdditionalSessionId = sessionId, AccessoryId = accessoryId, Quantity = quantity, Unit = unit, Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim() };
        db.CardioEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    private async Task<AdditionalSession> CreateAsync(AdditionalSession session, SessionType type, DateTime occurredOn, string? name, string? notes)
    {
        if (type == SessionType.ProgrammedWorkout) throw new ArgumentException("Additional sessions must be custom strength or cardio.", nameof(type));
        session.SessionType = type;
        session.OccurredOn = DateTime.SpecifyKind(occurredOn.Date, DateTimeKind.Utc);
        session.CreatedAt = DateTime.UtcNow;
        session.Name = string.IsNullOrWhiteSpace(name) ? type == SessionType.Cardio ? "Cardio Session" : "Custom Workout" : name.Trim();
        session.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        db.AdditionalSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    private IQueryable<AdditionalSession> OwnedQuery(string? userId)
    {
        return db.AdditionalSessions.Where(s =>
            (s.WeekId != null && s.Week!.Cycle.UserId == userId) ||
            (s.CycleId != null && s.Cycle!.UserId == userId) ||
            (s.PplWeekId != null && s.PplWeek!.Program.UserId == userId));
    }
}