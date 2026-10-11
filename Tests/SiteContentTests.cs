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
            Images =
            [
                new SiteContentImage { Url = "https://example.com/front.jpg", Caption = "Front view", SortOrder = 0 },
                new SiteContentImage { Url = "https://example.com/side.jpg", Caption = "Side view", SortOrder = 1 }
            ],
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
            .Include(entry => entry.Images)
            .Where(entry => entry.Status == SiteContentStatus.Published)
            .OrderByDescending(entry => entry.PublishedAtUtc)
            .ThenByDescending(entry => entry.Id)
            .FirstOrDefaultAsync();

        Assert.Collection(feed,
            entry => Assert.Equal(SiteContentType.ProductReview, entry.Type),
            entry => Assert.Equal(SiteContentType.BlogPost, entry.Type));
        Assert.Equal(recentReview.Id, latest?.Id);
        Assert.NotNull(latest);
        Assert.Collection(latest!.Images.OrderBy(image => image.SortOrder),
            image => Assert.Equal("Front view", image.Caption),
            image => Assert.Equal("Side view", image.Caption));
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
    public void SeoMetadataUsesSummaryOrReadableMarkdownDescription()
    {
        var content = new SiteContent
        {
            Summary = "",
            Body = "# Practical Strength\n\nA useful **training guide** for lifters."
        };

        var description = SiteContentSeo.GetDescription(content);

        Assert.Contains("Practical Strength", description);
        Assert.Contains("training guide", description);
        Assert.DoesNotContain("**", description);
        Assert.Equal(
            "https://example.com/reviews/steel-bar",
            SiteContentSeo.GetCanonicalUrl("https://example.com/", SiteContentType.ProductReview, "steel-bar"));
        Assert.Equal(
            "https://example.com/photos/front.jpg",
            SiteContentSeo.GetImageUrl("https://example.com", "/photos/front.jpg"));
    }

    [Fact]
    public void SitemapContainsOnlyPublishedBlogAndReviewUrls()
    {
        var publishedPost = new SiteContent
        {
            Type = SiteContentType.BlogPost,
            Status = SiteContentStatus.Published,
            Slug = "training-guide",
            UpdatedAtUtc = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)
        };
        var publishedReview = new SiteContent
        {
            Type = SiteContentType.ProductReview,
            Status = SiteContentStatus.Published,
            Slug = "bar-review"
        };
        var draft = new SiteContent
        {
            Type = SiteContentType.BlogPost,
            Status = SiteContentStatus.Draft,
            Slug = "private-draft"
        };

        var sitemap = SiteContentSitemap.Build("https://example.com", [publishedPost, publishedReview, draft]);
        var document = System.Xml.Linq.XDocument.Parse(sitemap);
        var urls = document.Descendants()
            .Where(element => element.Name.LocalName == "loc")
            .Select(element => element.Value)
            .ToList();

        Assert.Contains("https://example.com/blog/training-guide", urls);
        Assert.Contains("https://example.com/reviews/bar-review", urls);
        Assert.Contains("https://example.com/fitness-tracker", urls);
        Assert.DoesNotContain(urls, url => url.Contains("private-draft", StringComparison.Ordinal));
        Assert.Contains(document.Descendants().Where(element => element.Name.LocalName == "lastmod"),
            element => element.Value == "2026-10-10");
    }

    [Fact]
    public void RobotsFilePointsToSitemapAndExcludesPrivateApplicationRoutes()
    {
        var robots = SiteContentSeo.BuildRobotsTxt("https://example.com");

        Assert.Contains("Sitemap: https://example.com/sitemap.xml", robots);
        Assert.Contains("Disallow: /admin", robots);
        Assert.Contains("Disallow: /settings", robots);
        Assert.Contains("Allow: /blog", robots);
        Assert.Contains("Allow: /reviews", robots);
        Assert.Contains("Allow: /fitness-tracker", robots);
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
                Images = [new SiteContentImage { Url = "/images/lift.jpg", Caption = "A completed lift" }],
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
            .Include(item => item.Images)
            .Where(item => item.Status == SiteContentStatus.Published)
            .SingleAsync();
        var draft = await context.SiteContents
            .Where(item => item.Type == SiteContentType.ProductReview)
            .SingleAsync();

        Assert.Equal(SiteContentType.BlogPost, published.Type);
        Assert.Equal(SiteContentStatus.Published, published.Status);
        Assert.Equal("https://example.com/training.jpg", published.FeaturedImageUrl);
        Assert.Equal("Ready for the next set.", published.FeaturedImageCaption);
        Assert.Equal("A completed lift", published.Images.Single().Caption);
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
