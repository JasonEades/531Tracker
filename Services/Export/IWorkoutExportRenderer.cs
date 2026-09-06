namespace FiveThreeOneTracker.Services.Export;

public interface IWorkoutExportRenderer
{
    WorkoutExportFormat Format { get; }
    byte[] Render(WorkoutExportDocument document);
}
