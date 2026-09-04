using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FiveThreeOneTracker.Services;

public interface IProtocolService
{
    Task<UserProtocol> GetAsync();
    Task SaveAsync(string markdown);
}

public class ProtocolService(AppDbContext db, ICurrentUserService userContext) : IProtocolService
{
    public async Task<UserProtocol> GetAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        var protocol = await db.UserProtocols
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (protocol is not null)
            return protocol;

        protocol = new UserProtocol { UserId = userId };
        db.UserProtocols.Add(protocol);
        await db.SaveChangesAsync();
        return protocol;
    }

    public async Task SaveAsync(string markdown)
    {
        var protocol = await GetAsync();
        protocol.Markdown = markdown;
        protocol.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
