using System.Text;
using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IExerciseHistoryService
{
    Task<List<ExerciseSummary>> GetExerciseSummariesAsync();
    Task<ExerciseHistory?> GetExerciseHistoryAsync(string key);
}

public sealed class ExerciseHistoryService(
    AppDbContext db,
    ICurrentUserService userContext,
    IWeightCalculator weightCalculator) : IExerciseHistoryService
{
    public async Task<List<ExerciseSummary>> GetExerciseSummariesAsync()
    {
        var observations = await LoadObservationsAsync();

        return observations
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.Date).ToList();
                var latestDate = ordered[^1].Date.Date;
                return new ExerciseSummary
                {
                    Key = BuildKey(ordered[^1].Name),
                    Name = ordered[^1].Name,
                    LastPerformed = latestDate,
                    SessionCount = ordered.Select(x => x.Date.Date).Distinct().Count(),
                    Best1Rm = ordered.Max(x => x.Estimated1Rm),
                    HeaviestWeight = ordered.Max(x => x.Weight),
                    Latest1Rm = ordered.Where(x => x.Date.Date == latestDate).Max(x => x.Estimated1Rm),
                    Sources = ordered.Select(x => x.Source).Distinct().OrderBy(x => x).ToList()
                };
            })
            .OrderByDescending(x => x.LastPerformed)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public async Task<ExerciseHistory?> GetExerciseHistoryAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        var observations = await LoadObservationsAsync();
        var group = observations
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => string.Equals(BuildKey(g.Key), key, StringComparison.OrdinalIgnoreCase));
        if (group is null) return null;

        var ascending = group
            .GroupBy(x => x.Date.Date)
            .OrderBy(x => x.Key)
            .Select(session => new
            {
                Date = session.Key,
                Source = string.Join(" + ", session.Select(x => x.Source).Distinct().OrderBy(x => x)),
                Sets = session
                    .OrderBy(x => x.Order)
                    .Select(x => new ExerciseSetRecord
                    {
                        Weight = x.Weight,
                        Reps = x.Reps,
                        Estimated1Rm = x.Estimated1Rm,
                        SetLabel = x.SetLabel
                    })
                    .ToList()
            })
            .ToList();

        var sessions = new List<ExerciseSessionRecord>(ascending.Count);
        double bestSoFar = 0;
        for (var i = 0; i < ascending.Count; i++)
        {
            var current = ascending[i];
            var best1Rm = current.Sets.Count == 0 ? 0 : current.Sets.Max(x => x.Estimated1Rm);
            var volume = current.Sets.Sum(x => x.Weight * x.Reps);
            var previous = i == 0 ? null : ascending[i - 1];

            sessions.Add(new ExerciseSessionRecord
            {
                Date = current.Date,
                Source = current.Source,
                Sets = current.Sets,
                Change1RmVsPrevious = previous is null
                    ? null
                    : best1Rm - previous.Sets.Max(x => x.Estimated1Rm),
                ChangeVolumeVsPrevious = previous is null
                    ? null
                    : volume - previous.Sets.Sum(x => x.Weight * x.Reps),
                IsBest1Rm = best1Rm > bestSoFar
            });

            if (best1Rm > bestSoFar) bestSoFar = best1Rm;
        }

        sessions.Reverse();

        return new ExerciseHistory
        {
            Key = BuildKey(group.Key),
            Name = group.OrderBy(x => x.Date).Last().Name,
            Sessions = sessions
        };
    }

    private async Task<List<ExerciseObservation>> LoadObservationsAsync() =>
        await ExerciseObservations.LoadAsync(db, await userContext.GetUserIdAsync(), weightCalculator);

    /// <summary>URL-safe, stable identifier derived from the exercise name.</summary>
    public static string BuildKey(string name)
    {
        var builder = new StringBuilder(name.Length);
        var lastWasDash = false;
        foreach (var character in name.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
