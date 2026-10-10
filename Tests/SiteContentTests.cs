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
    public async Task BlogFeedIncludesPublishedPostsAndReviewsInMostRecentFirstOrder()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var olderPost = new SiteContent
        {
            Type = SiteContentType.BlogPost,
            Status = SiteContentStatus.Published,
            Title = "Training post",
            Slug = "training-post",
            Body = "Training notes.",
            PublishedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        var recentReview = new SiteContent
        {
            Type = SiteContentType.ProductReview,
            Status = SiteContentStatus.Published,
            Title = "New equipment review",
            Slug = "new-equipment-review",
            Body = "A review.",
            ProductName = "Barbell",
            Rating = 5,
            PublishedAtUtc = DateTime.UtcNow
        };
        var hiddenDraft = new SiteContent
        {
            Type = SiteContentType.ProductReview,
            Status = SiteContentStatus.Draft,
            Title = "Private draft",
            Slug = "private-draft",
            Body = "Not published."
        };
        context.SiteContents.AddRange(olderPost, recentReview, hiddenDraft);
        await context.SaveChangesAsync();

        var feed = await context.SiteContents.AsNoTracking()
            .Where(entry => entry.Status == SiteContentStatus.Published)
            .OrderByDescending(entry => entry.PublishedAtUtc)
            .ThenByDescending(entry => entry.Id)
            .ToListAsync();
        var latest = await context.SiteContents.AsNoTracking()
            .Where(entry => entry.Status == SiteContentStatus.Published)
            .OrderByDescending(entry => entry.PublishedAtUtc)
            .ThenByDescending(entry => entry.Id)
            .FirstOrDefaultAsync();

        Assert.Collection(feed,
            entry => Assert.Equal(SiteContentType.ProductReview, entry.Type),
            entry => Assert.Equal(SiteContentType.BlogPost, entry.Type));
        Assert.Equal(recentReview.Id, latest?.Id);
        Assert.DoesNotContain(feed, entry => entry.Status == SiteContentStatus.Draft);
    }

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
