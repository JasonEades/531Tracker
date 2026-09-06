using System.Globalization;
using System.Text;

namespace FiveThreeOneTracker.Services.Export;

public sealed class MarkdownWorkoutExporter : IWorkoutExportRenderer
{
    public WorkoutExportFormat Format => WorkoutExportFormat.Markdown;

    public byte[] Render(WorkoutExportDocument document) =>
        Encoding.UTF8.GetBytes(RenderText(document));

    private static string RenderText(WorkoutExportDocument document)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# 5/3/1 Tracker Workout Export");
        builder.AppendLine();
        builder.AppendLine($"**Program:** {document.ProgramName}");
        builder.AppendLine($"**Generated:** {document.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine();

        switch (document.Scope)
        {
            case WorkoutExportScope.Day:
                RenderWorkout(builder, document.Workout!, 1, true);
                break;
            case WorkoutExportScope.Week:
                RenderWeek(builder, document.Week!, 1);
                break;
            case WorkoutExportScope.Cycle:
                RenderCycle(builder, document.Cycle!);
                break;
        }

        return builder.ToString();
    }

    private static void RenderCycle(StringBuilder builder, CycleExportModel cycle)
    {
        builder.AppendLine($"# Training Cycle — {cycle.Name}");
        builder.AppendLine();
        builder.AppendLine($"**Cycle Number:** {cycle.CycleNumber}");
        builder.AppendLine($"**Created:** {cycle.CreatedAt:yyyy-MM-dd}");
        builder.AppendLine($"**Status:** {(cycle.IsCompleted ? "Completed" : "In progress")}");
        RenderNote(builder, "Cycle Notes", cycle.Notes);
        RenderSummary(builder, "Cycle Summary", cycle.Summary);

        foreach (var week in cycle.Weeks)
        {
            builder.AppendLine("---");
            builder.AppendLine();
            RenderWeek(builder, week, 1);
        }
    }

    private static void RenderWeek(StringBuilder builder, WeekExportModel week, int headingLevel)
    {
        var heading = new string('#', headingLevel + 1);
        builder.AppendLine($"{heading} Training Week — Week {week.WeekNumber}");
        builder.AppendLine();
        builder.AppendLine($"**Cycle:** Cycle {week.CycleNumber}");
        RenderNote(builder, "Week Notes", week.Notes);
        RenderSummary(builder, "Weekly Summary", week.Summary);

        foreach (var workout in week.Workouts)
        {
            builder.AppendLine("---");
            builder.AppendLine();
            RenderWorkout(builder, workout, headingLevel + 1, false);
        }
    }

    private static void RenderWorkout(StringBuilder builder, WorkoutExportModel workout, int headingLevel, bool includeCycleMetadata)
    {
        var heading = new string('#', headingLevel + 1);
        builder.AppendLine($"{heading} Workout — {workout.WorkoutName}");
        builder.AppendLine();
        builder.AppendLine($"**Date:** {(workout.WorkoutDate.HasValue ? workout.WorkoutDate.Value.ToString("yyyy-MM-dd") : "—")}");
        builder.AppendLine($"**Program:** 5/3/1");
        builder.AppendLine($"**Cycle:** Cycle {workout.CycleNumber}");
        builder.AppendLine($"**Week:** {workout.WeekNumber}");
        builder.AppendLine($"**Workout Type:** {workout.WorkoutType}");
        builder.AppendLine($"**Status:** {workout.Status}");
        RenderNote(builder, "Workout Notes", workout.Notes);
        RenderSummary(builder, "Workout Summary", workout.Summary);

        foreach (var exercise in workout.Exercises)
        {
            builder.AppendLine();
            builder.AppendLine($"{new string('#', headingLevel + 2)} {exercise.Category} — {exercise.Name}");
            RenderNote(builder, "Exercise Notes", exercise.Notes);
            builder.AppendLine();
            builder.AppendLine("| Set | Type | Target Reps | Actual Reps | Target Weight | Actual Weight | Completed | Notes |");
            builder.AppendLine("|---:|---|---:|---:|---:|---:|:---:|---|");
            foreach (var set in exercise.Sets)
            {
                var type = set.IsAmrap ? $"{set.Type} — AMRAP" : set.Type;
                builder.AppendLine($"| {set.Number} | {type} | {set.TargetReps} | {Value(set.ActualReps)} | {Weight(set.TargetWeight)} | {Weight(set.ActualWeight)} | {(set.IsCompleted ? "Yes" : "No")} | {Inline(set.Notes)} |");
            }
            if (exercise.AdditionalSets.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("### Additional Sets");
                builder.AppendLine();
                builder.AppendLine("| Set | Type | Weight | Reps | RPE | RIR | Notes |");
                builder.AppendLine("|---:|---|---:|---:|---:|---:|---|");
                foreach (var set in exercise.AdditionalSets)
                    builder.AppendLine($"| {set.Number} | {Inline(set.Type)} | {Weight(set.ActualWeight ?? set.TargetWeight)} | {Value(set.ActualReps ?? set.TargetReps)} | {Value(set.Rpe)} | {Value(set.Rir)} | {Inline(set.Notes)} |");
            }
        }

        if (workout.Accessories.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine($"{new string('#', headingLevel + 2)} Accessories");
            builder.AppendLine();
            builder.AppendLine("| Exercise | Sets | Reps | Weight | Completed | Notes |");
            builder.AppendLine("|---|---:|---:|---:|:---:|---|");
            foreach (var accessory in workout.Accessories)
            {
                builder.AppendLine($"| {Inline(accessory.Name)} | {accessory.Sets} | {accessory.Reps} | {Weight(accessory.Weight)} | {(accessory.IsCompleted ? "Yes" : "No")} | {Inline(accessory.Notes ?? accessory.Description)} |");
            }
        }
    }

    private static void RenderSummary(StringBuilder builder, string title, ExportSummaryModel summary)
    {
        builder.AppendLine();
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        builder.AppendLine($"- Workouts: {summary.CompletedWorkoutCount}/{summary.WorkoutCount} completed");
        builder.AppendLine($"- Exercises: {summary.ExerciseCount}");
        builder.AppendLine($"- Sets: {summary.CompletedSetCount}/{summary.SetCount} completed");
        builder.AppendLine($"- Total reps: {summary.TotalReps}");
        builder.AppendLine($"- Total volume/load: {Weight(summary.TotalVolume)}");
    }

    private static void RenderNote(StringBuilder builder, string title, string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;
        builder.AppendLine();
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        foreach (var line in note.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            builder.AppendLine($"> {line}");
    }

    private static string Inline(string? value) => string.IsNullOrEmpty(value) ? "—" : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", "<br>");
    private static string Value(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "—";
    private static string Value(double? value) => value?.ToString("0.#", CultureInfo.InvariantCulture) ?? "—";
    private static string Weight(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Weight(double? value) => value.HasValue ? Weight(value.Value) : "—";
}
