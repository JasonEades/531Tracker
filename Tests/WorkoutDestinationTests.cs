using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class WorkoutDestinationTests
{
    private static Cycle CycleWith(params (int Id, WeekNumber Week, LiftType Lift, WorkoutStatus Status)[] workouts) => new()
    {
        Id = 7,
        Weeks = workouts.GroupBy(w => w.Week).Select(g => new Week
        {
            WeekNumber = g.Key,
            Workouts = g.Select(w => new Workout { Id = w.Id, MainLiftType = w.Lift, Status = w.Status }).ToList()
        }).ToList()
    };

    private static PplWeekPlan PlanWith(PplDayState state, int? sessionId, bool loggedSets) =>
        new(1, [new PplWeekDay(1, 1, "Push", PplDayType.Push, state, sessionId, loggedSets)], 1);

    [Fact]
    public void FiveThreeOneOpensNextUnfinishedWorkoutInsteadOfCyclePage()
    {
        var cycle = CycleWith(
            (2, WeekNumber.Week2, LiftType.Squat, WorkoutStatus.NotStarted),
            (1, WeekNumber.Week1, LiftType.Squat, WorkoutStatus.Completed),
            (3, WeekNumber.Week1, LiftType.Deadlift, WorkoutStatus.NotStarted));

        Assert.Equal("/workout/3", WorkoutDestination.Resolve(cycle, null, null));
    }

    [Fact]
    public void InProgressFiveThreeOneWorkoutWins()
    {
        var cycle = CycleWith(
            (1, WeekNumber.Week1, LiftType.Squat, WorkoutStatus.NotStarted),
            (2, WeekNumber.Week1, LiftType.Deadlift, WorkoutStatus.InProgress));

        Assert.Equal("/workout/2", WorkoutDestination.Resolve(cycle, new PplProgram { Id = 4 }, PlanWith(PplDayState.Open, 9, true)));
    }

    [Fact]
    public void PplSessionWithLoggedSetsWinsOverUnstartedCycle()
    {
        var cycle = CycleWith((1, WeekNumber.Week1, LiftType.Squat, WorkoutStatus.NotStarted));

        Assert.Equal("/ppl/session/9", WorkoutDestination.Resolve(cycle, new PplProgram { Id = 4 }, PlanWith(PplDayState.Open, 9, true)));
    }

    [Fact]
    public void PplOnlyUserOpensOpenSessionOrProgramPage()
    {
        var program = new PplProgram { Id = 4 };

        Assert.Equal("/ppl/session/9", WorkoutDestination.Resolve(null, program, PlanWith(PplDayState.Open, 9, false)));
        Assert.Equal("/ppl/4", WorkoutDestination.Resolve(null, program, PlanWith(PplDayState.Available, null, false)));
        Assert.Equal("/ppl/4", WorkoutDestination.Resolve(null, program, null));
    }

    [Fact]
    public void NoActiveProgramGoesToDashboardNotHistory()
    {
        Assert.Equal("/", WorkoutDestination.Resolve(null, null, null));
    }
}
