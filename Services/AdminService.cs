using FiveThreeOneTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IAdminService
{
    Task<List<AdminUserDto>> GetAllUsersAsync();
    Task<bool> SetUserEnabledAsync(string userId, bool enabled);
}

public record AdminUserDto(
    string Id,
    string Email,
    string? DisplayName,
    bool IsEnabled,
    bool IsAdmin,
    DateTimeOffset? LockoutEnd);

public class AdminService(UserManager<ApplicationUser> userManager) : IAdminService
{
    public async Task<List<AdminUserDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();

        var result = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var isAdmin = await userManager.IsInRoleAsync(user, "Admin");
            result.Add(new AdminUserDto(
                user.Id,
                user.Email ?? "(no email)",
                user.DisplayName,
                user.IsEnabled,
                isAdmin,
                user.LockoutEnd));
        }
        return result;
    }

    public async Task<bool> SetUserEnabledAsync(string userId, bool enabled)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        user.IsEnabled = enabled;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded;
    }
}
