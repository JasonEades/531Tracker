using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace FiveThreeOneTracker.Services;

public interface ICurrentUserService
{
    /// <summary>
    /// Returns the current authenticated user's ID, or throws if not authenticated.
    /// </summary>
    Task<string> GetUserIdAsync();

    /// <summary>
    /// Returns the current user's ID, or null if not authenticated.
    /// </summary>
    Task<string?> GetUserIdOrNullAsync();
}

public class CurrentUserService(AuthenticationStateProvider authStateProvider) : ICurrentUserService
{
    public async Task<string> GetUserIdAsync()
    {
        var userId = await GetUserIdOrNullAsync();
        if (userId is null)
            throw new InvalidOperationException("User is not authenticated.");
        return userId;
    }

    public async Task<string?> GetUserIdOrNullAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
