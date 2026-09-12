using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace FiveThreeOneTracker.Services.Export;

public sealed class PdfWorkoutExporter : IWorkoutExportRenderer
{
    public WorkoutExportFormat Format => WorkoutExportFormat.Pdf;

    public byte[] Render(WorkoutExportDocument document)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Element(header => RenderHeader(header, document));
                page.Content().Element(content => RenderContent(content, document));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated ");
                    text.Span(document.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    text.Span(" UTC  •  Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void RenderHeader(IContainer container, WorkoutExportDocument document)
    {
        container.Column(column =>
        {
            column.Item().Text("The Lifting Lab Workout Export").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(4).Text($"Program: {document.ProgramName}").FontSize(10);
        });
    }

    private static void RenderContent(IContainer container, WorkoutExportDocument document)
    {
        container.Column(column =>
        {
            switch (document.Scope)
            {
                case WorkoutExportScope.Day:
                    RenderWorkout(column.Item(), document.Workout!);
                    break;
                case WorkoutExportScope.Week:
                    RenderWeek(column.Item(), document.Week!);
                    break;
                case WorkoutExportScope.Cycle:
                    RenderCycle(column.Item(), document.Cycle!);
                    break;
            }
        });
    }

    private static void RenderCycle(IContainer container, CycleExportModel cycle)
    {
        container.Column(column =>
        {
            SectionTitle(column.Item(), $"Training Cycle — {cycle.Name}", 18);
            Metadata(column.Item(), $"Cycle {cycle.CycleNumber}  |  Created {cycle.CreatedAt:yyyy-MM-dd}  |  {(cycle.IsCompleted ? "Completed" : "In progress")}");
            Note(column.Item(), "Cycle Notes", cycle.Notes);
            Summary(column.Item(), "Cycle Summary", cycle.Summary, cycle.DailySteps);
            foreach (var session in cycle.AdditionalSessions)
            {
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                RenderAdditionalSession(column.Item().PaddingTop(8), session);
            }
            foreach (var week in cycle.Weeks)
            {
                column.Item().PaddingTop(14).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                RenderWeek(column.Item().PaddingTop(8), week);
            }
        });
    }

    private static void RenderWeek(IContainer container, WeekExportModel week)
    {
        container.Column(column =>
        {
            SectionTitle(column.Item(), $"Training Week — Week {week.WeekNumber}", 16);
            Metadata(column.Item(), $"Cycle {week.CycleNumber}");
            Note(column.Item(), "Week Notes", week.Notes);
            Summary(column.Item(), "Weekly Summary", week.Summary, week.DailySteps);
            foreach (var workout in week.Workouts)
            {
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                RenderWorkout(column.Item().PaddingTop(8), workout);
            }
            foreach (var session in week.AdditionalSessions)
            {
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                RenderAdditionalSession(column.Item().PaddingTop(8), session);
            }
        });
    }

    private static void RenderAdditionalSession(IContainer container, AdditionalSessionExportModel session)
    {
        container.Column(column =>
        {
            SectionTitle(column.Item(), $"Additional Session — {session.Name}", 14);
            Metadata(column.Item(), $"Date: {session.Date:yyyy-MM-dd}  |  Type: {session.SessionType}");
            Note(column.Item(), "Session Notes", session.Notes);
            if (session.CardioEntries.Count > 0)
            {
                column.Item().PaddingTop(8).Text("Cardio").Bold().FontSize(11);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(3); columns.ConstantColumn(65); columns.RelativeColumn(2); columns.RelativeColumn(3); });
                    Header(table, "Exercise", "Quantity", "Unit", "Notes");
                    foreach (var entry in session.CardioEntries)
                    {
                        table.Cell().Element(Cell).Text(entry.Exercise);
                        table.Cell().Element(Cell).Text(entry.Quantity.ToString("0.##"));
                        table.Cell().Element(Cell).Text(entry.Unit);
                        table.Cell().Element(Cell).Text(entry.Notes ?? "");
                    }
                });
            }
            foreach (var exercise in session.Exercises)
            {
                column.Item().PaddingTop(8).Text(exercise.Name).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.ConstantColumn(35); columns.ConstantColumn(65); columns.ConstantColumn(45); columns.RelativeColumn(3); });
                    Header(table, "Set", "Weight", "Reps", "Notes");
                    foreach (var set in exercise.Sets)
                    {
                        table.Cell().Element(Cell).Text(set.Number.ToString());
                        table.Cell().Element(Cell).Text(set.Weight?.ToString("0.##") ?? "—");
                        table.Cell().Element(Cell).Text(set.Reps?.ToString() ?? "—");
                        table.Cell().Element(Cell).Text(set.Notes ?? "");
                    }
                });
            }
        });
    }

    private static void RenderWorkout(IContainer container, WorkoutExportModel workout)
    {
        container.Column(column =>
        {
            SectionTitle(column.Item(), $"Workout — {workout.WorkoutName}", 14);
            Metadata(column.Item(), $"Date: {(workout.WorkoutDate?.ToString("yyyy-MM-dd") ?? "—")}  |  Cycle: {workout.CycleNumber}  |  Week: {workout.WeekNumber}  |  Status: {workout.Status}");
            Note(column.Item(), "Workout Notes", workout.Notes);
            foreach (var exercise in workout.Exercises)
            {
                column.Item().PaddingTop(8).Text($"{exercise.Category} — {exercise.Name}").Bold().FontSize(11);
                Note(column.Item(), "Exercise Notes", exercise.Notes);
                RenderSets(column.Item(), exercise);
            }
            if (workout.Accessories.Count > 0)
            {
                column.Item().PaddingTop(8).Text("Accessories").Bold().FontSize(11);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => { columns.RelativeColumn(3); columns.ConstantColumn(40); columns.ConstantColumn(40); columns.ConstantColumn(55); columns.ConstantColumn(55); columns.RelativeColumn(3); });
                    Header(table, "Exercise", "Sets", "Reps", "Weight", "Done", "Notes");
                    foreach (var accessory in workout.Accessories)
                    {
                        table.Cell().Element(Cell).Text(text => text.Span($"{accessory.Name}{(string.IsNullOrWhiteSpace(accessory.Description) ? "" : $" — {accessory.Description}")}"));
                        table.Cell().Element(Cell).Text(accessory.Sets.ToString());
                        table.Cell().Element(Cell).Text(accessory.Reps.ToString());
                        table.Cell().Element(Cell).Text(Weight(accessory.Weight));
                        table.Cell().Element(Cell).Text(accessory.IsCompleted ? "Yes" : "No");
                        table.Cell().Element(Cell).Text(accessory.Notes ?? "");
                    }
                });
            }

            Summary(column.Item(), "Workout Summary", workout.Summary);
        });
    }

    private static void RenderSets(IContainer container, ExerciseExportModel exercise)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28); columns.ConstantColumn(65); columns.ConstantColumn(48); columns.ConstantColumn(48);
                columns.ConstantColumn(62); columns.ConstantColumn(62); columns.ConstantColumn(48); columns.RelativeColumn();
            });
            Header(table, "Set", "Type", "Target", "Actual", "Target Wt", "Actual Wt", "Done", "Notes");
            foreach (var set in exercise.Sets)
            {
                table.Cell().Element(Cell).Text(set.Number.ToString());
                table.Cell().Element(Cell).Text(set.IsAmrap ? $"{set.Type} AMRAP" : set.Type);
                table.Cell().Element(Cell).Text(set.TargetReps.ToString());
                table.Cell().Element(Cell).Text(set.ActualReps?.ToString() ?? "—");
                table.Cell().Element(Cell).Text(Weight(set.TargetWeight));
                table.Cell().Element(Cell).Text(Weight(set.ActualWeight));
                table.Cell().Element(Cell).Text(set.IsCompleted ? "Yes" : "No");
                table.Cell().Element(Cell).Text(set.Notes ?? "—");
            }
            foreach (var set in exercise.AdditionalSets)
            {
                table.Cell().Element(Cell).Text(set.Number.ToString());
                table.Cell().Element(Cell).Text(set.Type);
                table.Cell().Element(Cell).Text("—");
                table.Cell().Element(Cell).Text((set.ActualReps ?? set.TargetReps).ToString());
                table.Cell().Element(Cell).Text("—");
                table.Cell().Element(Cell).Text(Weight(set.ActualWeight ?? set.TargetWeight));
                table.Cell().Element(Cell).Text("Yes");
                table.Cell().Element(Cell).Text(AdditionalSetNotes(set));
            }
        });
    }

    private static string AdditionalSetNotes(SetExportModel set)
    {
        var notes = new List<string>();
        if (set.Rpe.HasValue) notes.Add($"RPE {set.Rpe:0.#}");
        if (set.Rir.HasValue) notes.Add($"RIR {set.Rir:0.#}");
        if (!string.IsNullOrWhiteSpace(set.Notes)) notes.Add(set.Notes);
        return notes.Count == 0 ? "—" : string.Join(" · ", notes);
    }

    private static void Summary(
        IContainer container,
        string title,
        ExportSummaryModel summary,
        IEnumerable<DailyStepExportModel>? dailySteps = null)
    {
        container.PaddingTop(6).Background(Colors.Grey.Lighten4).Padding(7).Column(column =>
        {
            column.Item().Text(title).Bold();
            column.Item().Text($"Workouts: {summary.CompletedWorkoutCount}/{summary.WorkoutCount}  |  Exercises: {summary.ExerciseCount}  |  Sets: {summary.CompletedSetCount}/{summary.SetCount}  |  Reps: {summary.TotalReps}  |  Volume: {Weight(summary.TotalVolume)}");
            if (dailySteps is not null)
            {
                var steps = dailySteps.ToList();
                var totalSteps = steps.Sum(x => x.Steps);
                var averageDailySteps = steps.Count == 0 ? 0 : steps.Average(x => x.Steps);
                column.Item().Text($"Steps: {totalSteps:N0}  |  Avg Daily Steps: {averageDailySteps:N0}");
            }
        });
    }

    private static void Note(IContainer container, string title, string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;
        container.PaddingTop(5).Background(Colors.Yellow.Lighten4).Border(1).BorderColor(Colors.Yellow.Darken1).Padding(6).Column(column =>
        {
            column.Item().Text(title).Bold().FontColor(Colors.Brown.Darken2);
            foreach (var line in note.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
                column.Item().Text(line);
        });
    }

    private static void SectionTitle(IContainer container, string title, float size) => container.PaddingTop(4).Text(title).FontSize(size).Bold();
    private static void Metadata(IContainer container, string value) => container.PaddingTop(3).Text(value).FontSize(9).FontColor(Colors.Grey.Darken2);
    private static void Header(TableDescriptor table, params string[] values) { foreach (var value in values) table.Cell().Element(HeaderCell).Text(value).Bold(); }
    private static IContainer HeaderCell(IContainer container) => container.Background(Colors.Blue.Darken2).Padding(4);
    private static IContainer Cell(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
    private static string Weight(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Weight(double? value) => value.HasValue ? Weight(value.Value) : "—";
}
