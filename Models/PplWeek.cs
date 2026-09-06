namespace FiveThreeOneTracker.Models;

public class PplWeek
{
    public int Id { get; set; }
    public int PplProgramId { get; set; }
    public PplProgram Program { get; set; } = null!;
    public int WeekNumber { get; set; }
    public DateTime StartDate { get; set; }
    public ICollection<PplSession> Sessions { get; set; } = [];
    public ICollection<AdditionalSession> AdditionalSessions { get; set; } = [];
}