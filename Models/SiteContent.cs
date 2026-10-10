using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public enum SiteContentType
{
    BlogPost,
    ProductReview
}

public enum SiteContentStatus
{
    Draft,
    Published
}

public class SiteContent
{
    public int Id { get; set; }

    public SiteContentType Type { get; set; }

    public SiteContentStatus Status { get; set; } = SiteContentStatus.Draft;

    [Required, MaxLength(160)]
    public string Title { get; set; } = "";

    [Required, MaxLength(180)]
    public string Slug { get; set; } = "";

    [MaxLength(400)]
    public string Summary { get; set; } = "";

    [MaxLength(1000)]
    public string? FeaturedImageUrl { get; set; }

    [MaxLength(240)]
    public string? FeaturedImageCaption { get; set; }

    [Required]
    public string Body { get; set; } = "";

    [MaxLength(160)]
    public string? ProductName { get; set; }

    [MaxLength(120)]
    public string? Brand { get; set; }

    public int? Rating { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAtUtc { get; set; }

    public List<SiteContentImage> Images { get; set; } = [];
}

public class SiteContentImage
{
    public int Id { get; set; }

    public int SiteContentId { get; set; }

    public SiteContent SiteContent { get; set; } = null!;

    [Required, MaxLength(1000)]
    public string Url { get; set; } = "";

    [MaxLength(240)]
    public string? Caption { get; set; }

    public int SortOrder { get; set; }
}
