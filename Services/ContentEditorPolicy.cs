using System.Security.Claims;

namespace FiveThreeOneTracker.Services;

public static class ContentEditorPolicy
{
    public const string Name = "ContentEditor";
    public const string OwnerEmail = "jason.eades1@gmail.com";

    public static bool IsAuthorized(ClaimsPrincipal principal) =>
        principal.IsInRole("Admin") &&
        principal.Claims.Any(claim =>
            (claim.Type == ClaimTypes.Email || claim.Type == "email") &&
            string.Equals(claim.Value, OwnerEmail, StringComparison.OrdinalIgnoreCase));
}
