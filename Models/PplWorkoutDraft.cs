namespace FiveThreeOneTracker.Models;

public sealed record PplWorkoutDayDraft(
    int DayTemplateId,
    string Name,
    IReadOnlyList<PplWorkoutExerciseDraft> Exercises);

public sealed record PplWorkoutExerciseDraft(
    int? SourceSlotId,
    string ExerciseName,
    MuscleGroup MuscleGroup,
    int TargetSets,
    int RepsMin,
    int RepsMax,
    bool IsBodyweight,
    double ProgressionIncrement,
    double? StartingWeight,
    double? CurrentWeight,
    int? ExerciseDefinitionId = null);
