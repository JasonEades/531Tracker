namespace FiveThreeOneTracker.Models;

public enum LiftType
{
    Squat,
    BenchPress,
    Deadlift,
    OverheadPress
}

public enum WeekNumber
{
    Week1 = 1,
    Week2 = 2,
    Week3 = 3,
    Week4 = 4
}

public enum WorkoutStatus
{
    NotStarted,
    InProgress,
    Completed
}

public enum SetType
{
    Main,
    Bbb,
    Warmup
}

public enum BbbMode
{
    None,
    OppositeDay,
    SameDay
}
