using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public sealed record CycleWeekAssignment(Cycle Cycle, Week Week);

public interface ICycleDateResolver
{
    Task<CycleWeekAssignment?> FindAssignmentAsync(string userId, DateTime localDate);
}

public sealed class CycleDateResolver(AppDbContext db) : ICycleDateResolver
{
    public async Task<CycleWeekAssignment?> FindAssignmentAsync(string userId, DateTime localDate)
    {
        var date = localDate.Date;
        var cycles = await db.Cycles
            .Include(c => c.Weeks)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var match = cycles
            .Select(c => new
            {
                Cycle = c,
                Start = (c.StartDate ?? c.CreatedAt).Date,
                End = (c.StartDate ?? c.CreatedAt).Date.AddDays(28)
            })
            .Where(x => date >= x.Start && date < x.End)
            .OrderByDescending(x => x.Start)
            .FirstOrDefault();

        if (match is null)
            return null;

        var weekNumber = (WeekNumber)((date - match.Start).Days / 7 + 1);
        var week = match.Cycle.Weeks.SingleOrDefault(w => w.WeekNumber == weekNumber);
        return week is null ? null : new CycleWeekAssignment(match.Cycle, week);
    }
}
