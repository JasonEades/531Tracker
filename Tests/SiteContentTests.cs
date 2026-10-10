using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using FiveThreeOneTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FiveThreeOneTracker.Tests;

public sealed class SiteContentTests
{
    [Fact]
    public void MarkdownOutputDoesNotRenderRawHtmlOrDangerousLinks()
    {
        var html = SiteContentMarkdown.ToSafeHtml(
            "<script>alert('xss')</script>\n\n[bad](javascript:alert(1)) [safe](https://example.com)");

        Assert.DoesNotContain("<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"https://example.com\"", html);
    }

    [Fact]
    public async Task ContentTypesStatusesAndUniqueSlugsPersist()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.SiteContents.AddRange(
            new SiteContent
            {
                Type = SiteContentType.BlogPost,
                Status = SiteContentStatus.Published,
                Title = "Published training notes",
                Slug = "published-training-notes",
                Body = "A training article.",
                FeaturedImageUrl = "https://example.com/training.jpg",
                FeaturedImageCaption = "Ready for the next set.",
                PublishedAtUtc = DateTime.UtcNow
            },
            new SiteContent
            {
                Type = SiteContentType.ProductReview,
                Status = SiteContentStatus.Draft,
                Title = "Draft barbell review",
                Slug = "draft-barbell-review",
                Body = "A product review draft.",
                ProductName = "Training Bar",
                Rating = 4
            });
        await context.SaveChangesAsync();

        var published = await context.SiteContents
            .Where(item => item.Status == SiteContentStatus.Published)
            .SingleAsync();
        var draft = await context.SiteContents
            .Where(item => item.Type == SiteContentType.ProductReview)
            .SingleAsync();

        Assert.Equal(SiteContentType.BlogPost, published.Type);
        Assert.Equal(SiteContentStatus.Published, published.Status);
        Assert.Equal("https://example.com/training.jpg", published.FeaturedImageUrl);
        Assert.Equal("Ready for the next set.", published.FeaturedImageCaption);
        Assert.Equal(SiteContentType.ProductReview, draft.Type);
        Assert.Equal(SiteContentStatus.Draft, draft.Status);
        Assert.Equal(4, draft.Rating);

        context.SiteContents.Add(new SiteContent
        {
            Type = SiteContentType.BlogPost,
            Title = "Duplicate slug",
            Slug = "published-training-notes",
            Body = "This slug is already taken."
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
