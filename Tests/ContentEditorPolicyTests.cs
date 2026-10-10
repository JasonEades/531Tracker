using System.Security.Claims;
using FiveThreeOneTracker.Services;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class ContentEditorPolicyTests
{
    [Fact]
    public void OwnerCanEditWhenAuthenticatedAsAdmin()
    {
        var user = CreateUser(ContentEditorPolicy.OwnerEmail, isAdmin: true);

        Assert.True(ContentEditorPolicy.IsAuthorized(user));
    }

    [Theory]
    [InlineData("someone-else@example.com", true)]
    [InlineData("jason.eades1@gmail.com", false)]
    [InlineData("", true)]
    public void NonOwnerOrNonAdminCannotEdit(string email, bool isAdmin)
    {
        var user = CreateUser(email, isAdmin);

        Assert.False(ContentEditorPolicy.IsAuthorized(user));
    }

    [Fact]
    public void OwnerEmailComparisonIsCaseInsensitive()
    {
        var user = CreateUser("JASON.EADES1@GMAIL.COM", isAdmin: true);

        Assert.True(ContentEditorPolicy.IsAuthorized(user));
    }

    private static ClaimsPrincipal CreateUser(string email, bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
