using Microsoft.AspNetCore.Identity;

namespace FiveThreeOneTracker.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public string? ProfilePictureUrl { get; set; }

    /// <summary>When false the user is blocked from signing in.</summary>
    public bool IsEnabled { get; set; } = true;
}
