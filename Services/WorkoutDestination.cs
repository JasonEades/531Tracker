using FiveThreeOneTracker.Models;

namespace FiveThreeOneTracker.Services;

/// <summary>Chooses where the Workout nav tab (/workout) lands so it opens a workout rather than the History pages.</summary>
public static class WorkoutDestination
{
    public static string Resolve(Cycle? currentCycle, PplProgram? activePplProgram, PplWeekPlan? pplWeekPlan)
    {
        var cycleWorkouts = currentCycle?.Weeks
            .OrderBy(week => week.WeekNumber)
            .SelectMany(week => week.Workouts.OrderBy(workout => workout.MainLiftType))
            .ToList() ?? [];

        var inProgressWorkout = cycleWorkouts.FirstOrDefault(workout => workout.Status == WorkoutStatus.InProgress);
        if (inProgressWorkout is not null)
            return $"/workout/{inProgressWorkout.Id}";

        var openPplDay = pplWeekPlan?.OpenDay;
        if (activePplProgram is not null && openPplDay?.SessionId is int openSessionId && openPplDay.HasLoggedSets)
            return $"/ppl/session/{openSessionId}";

        if (currentCycle is not null)
        {
            var nextWorkout = cycleWorkouts.FirstOrDefault(workout => workout.Status != WorkoutStatus.Completed);
            return nextWorkout is not null ? $"/workout/{nextWorkout.Id}" : $"/cycles/{currentCycle.Id}";
        }

        if (activePplProgram is not null)
            return openPplDay?.SessionId is int sessionId ? $"/ppl/session/{sessionId}" : $"/ppl/{activePplProgram.Id}";

        return "/";
    }
}
