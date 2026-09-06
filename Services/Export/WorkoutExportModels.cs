namespace FiveThreeOneTracker.Services.Export;

public sealed class WorkoutExportRequest
{
    public WorkoutExportScope Scope { get; init; }
    public WorkoutExportFormat Format { get; init; }
    public int Id { get; init; }
}

public sealed class WorkoutExportResult
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}

public sealed class WorkoutExportDocument
{
    public string AppName { get; init; } = "5/3/1 Tracker";
    public string ProgramName { get; init; } = "5/3/1";
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public WorkoutExportScope Scope { get; init; }
    public CycleExportModel? Cycle { get; init; }
    public WeekExportModel? Week { get; init; }
    public WorkoutExportModel? Workout { get; init; }
}

public sealed class CycleExportModel
{
    public int Id { get; init; }
    public int CycleNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsCompleted { get; init; }
    public string? Notes { get; init; }
    public List<WeekExportModel> Weeks { get; init; } = [];
    public ExportSummaryModel Summary { get; init; } = new();
}

public sealed class WeekExportModel
{
    public int Id { get; init; }
    public int WeekNumber { get; init; }
    public int CycleNumber { get; init; }
    public string? Notes { get; init; }
    public List<WorkoutExportModel> Workouts { get; init; } = [];
    public ExportSummaryModel Summary { get; init; } = new();
}

public sealed class WorkoutExportModel
{
    public int Id { get; init; }
    public int CycleNumber { get; init; }
    public int WeekNumber { get; init; }
    public string WorkoutName { get; init; } = string.Empty;
    public string WorkoutType { get; init; } = string.Empty;
    public DateTime? WorkoutDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public List<ExerciseExportModel> Exercises { get; init; } = [];
    public List<AccessoryExportModel> Accessories { get; init; } = [];
    public ExportSummaryModel Summary { get; init; } = new();
}

public sealed class ExerciseExportModel
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public List<SetExportModel> Sets { get; init; } = [];
}

public sealed class SetExportModel
{
    public int Number { get; init; }
    public string Type { get; init; } = string.Empty;
    public int TargetReps { get; init; }
    public double TargetWeight { get; init; }
    public int? ActualReps { get; init; }
    public double? ActualWeight { get; init; }
    public bool IsCompleted { get; init; }
    public string? Notes { get; init; }
    public bool IsAmrap { get; init; }
}

public sealed class AccessoryExportModel
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Notes { get; init; }
    public int Sets { get; init; }
    public int Reps { get; init; }
    public double Weight { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class ExportSummaryModel
{
    public int ExerciseCount { get; init; }
    public int SetCount { get; init; }
    public int CompletedSetCount { get; init; }
    public int TotalReps { get; init; }
    public double TotalVolume { get; init; }
    public int WorkoutCount { get; init; }
    public int CompletedWorkoutCount { get; init; }
}
