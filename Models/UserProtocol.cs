namespace FiveThreeOneTracker.Models;

public class UserProtocol
{
    public int Id { get; set; }

    /// <summary>Owner user ID (Identity).</summary>
    public string UserId { get; set; } = string.Empty;

    public string Markdown { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
