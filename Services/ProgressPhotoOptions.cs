namespace FiveThreeOneTracker.Services;

/// <summary>
/// Processing and retention limits for progress photos. Photos are stored in the database as
/// compressed JPEGs at a bounded resolution, and each user is capped to a fixed number of
/// photos, so the storage footprint per user has a hard ceiling.
/// </summary>
public sealed class ProgressPhotoOptions
{
    public const string SectionName = "ProgressPhotos";

    /// <summary>Maximum photos retained per user. Uploads beyond this are rejected.</summary>
    public int MaxPhotosPerUser { get; set; } = 30;

    /// <summary>Largest accepted upload before processing, in megabytes.</summary>
    public int MaxUploadMegabytes { get; set; } = 15;

    /// <summary>Longest edge of the stored image, in pixels. Larger images are downscaled.</summary>
    public int MaxDimension { get; set; } = 1280;

    /// <summary>Longest edge of the generated gallery thumbnail, in pixels.</summary>
    public int ThumbnailDimension { get; set; } = 320;

    /// <summary>JPEG quality (1-100) used for the stored image.</summary>
    public int JpegQuality { get; set; } = 78;

    /// <summary>JPEG quality (1-100) used for the thumbnail.</summary>
    public int ThumbnailJpegQuality { get; set; } = 65;
}
