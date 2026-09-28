namespace FiveThreeOneTracker.Models;

/// <summary>One completed working set, normalized across 5/3/1, PPL and additional strength sources.</summary>
public sealed class ExerciseSetRecord
{
    public double Weight { get; init; }
    public int Reps { get; init; }
    public double Estimated1Rm { get; init; }
    public string? SetLabel { get; init; }
    public double Volume => Weight * Reps;
}

/// <summary>All work performed for a single exercise on a single day.</summary>
public sealed class ExerciseSessionRecord
{
    public DateTime Date { get; init; }
    public string Source { get; init; } = string.Empty;
    public IReadOnlyList<ExerciseSetRecord> Sets { get; init; } = [];

    public int SetCount => Sets.Count;
    public int TotalReps => Sets.Sum(x => x.Reps);
    public double Volume => Sets.Sum(x => x.Volume);
    public double Best1Rm => Sets.Count == 0 ? 0 : Sets.Max(x => x.Estimated1Rm);
    public double HeaviestWeight => Sets.Count == 0 ? 0 : Sets.Max(x => x.Weight);

    /// <summary>Change in best estimated 1RM against the previous session for this exercise.</summary>
    public double? Change1RmVsPrevious { get; init; }

    /// <summary>Change in total volume against the previous session for this exercise.</summary>
    public double? ChangeVolumeVsPrevious { get; init; }

    /// <summary>True when this session produced the best estimated 1RM recorded so far.</summary>
    public bool IsBest1Rm { get; init; }
}

/// <summary>Row in the exercise index listing everything the user has actually trained.</summary>
public sealed class ExerciseSummary
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime LastPerformed { get; init; }
    public int SessionCount { get; init; }
    public double Best1Rm { get; init; }
    public double HeaviestWeight { get; init; }
    public double Latest1Rm { get; init; }
    public IReadOnlyList<string> Sources { get; init; } = [];
}

/// <summary>Full drill-down for a single exercise.</summary>
public sealed class ExerciseHistory
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<ExerciseSessionRecord> Sessions { get; init; } = [];

    public double Best1Rm => Sessions.Count == 0 ? 0 : Sessions.Max(x => x.Best1Rm);
    public double HeaviestWeight => Sessions.Count == 0 ? 0 : Sessions.Max(x => x.HeaviestWeight);
    public int MostReps => Sessions.Count == 0 ? 0 : Sessions.SelectMany(x => x.Sets).Max(x => x.Reps);
    public double BestVolume => Sessions.Count == 0 ? 0 : Sessions.Max(x => x.Volume);

    /// <summary>Most recent session first.</summary>
    public ExerciseSessionRecord? Latest => Sessions.Count == 0 ? null : Sessions[0];

    public ExerciseSessionRecord? Previous => Sessions.Count < 2 ? null : Sessions[1];
}
