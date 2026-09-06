namespace FiveThreeOneTracker.Models;

public enum LiftType
{
    Squat,
    BenchPress,
    Deadlift,
    OverheadPress
}

public enum SessionType
{
    ProgrammedWorkout,
    CustomStrength,
    Cardio
}

public enum AccessoryCategory
{
    Strength,
    Isolation,
    Core,
    Cardio,
    Other
}

public enum CardioUnit
{
    Minutes,
    Hours,
    Miles,
    Kilometers,
    Meters,
    Yards,
    Steps,
    Calories
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
    Warmup,
    Fsl
}

public enum AdditionalSetType
{
    Additional,
    DropSet,
    BackOffSet,
    ExtraVolume,
    Other
}

public enum BbbMode
{
    None,
    OppositeDay,
    SameDay
}

// ── PPL ──────────────────────────────────────────────────────────────────────

public enum PplDayType { Push, Pull, Legs }

public enum PplVariant { Single, A, B }

public enum MuscleGroup
{
    Chest, Shoulders, Triceps,
    Back, RearDelts, Biceps,
    Quads, Hamstrings, Glutes, Calves, Core
}
