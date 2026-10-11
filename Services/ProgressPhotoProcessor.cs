using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace FiveThreeOneTracker.Services;

public sealed record ProcessedPhoto(
    byte[] Image,
    byte[] Thumbnail,
    int Width,
    int Height);

/// <summary>Resizes and re-encodes uploads so stored photos stay small regardless of source size.</summary>
public interface IProgressPhotoProcessor
{
    ProcessedPhoto Process(Stream upload);

    /// <summary>Rotates an already-stored image by 90 degrees and regenerates its thumbnail.</summary>
    ProcessedPhoto Rotate(byte[] stored, bool clockwise);

    /// <summary>
    /// Returns a sharper thumbnail when the stored one is smaller than the configured size
    /// (photos uploaded before the thumbnail size was raised), or null if it is already adequate.
    /// </summary>
    byte[]? UpgradeThumbnail(byte[] storedThumbnail, byte[] storedImage);
}

public sealed class ProgressPhotoProcessor(IOptions<ProgressPhotoOptions> options) : IProgressPhotoProcessor
{
    private readonly ProgressPhotoOptions _options = options.Value;

    public ProcessedPhoto Process(Stream upload)
    {
        using var image = Image.Load(upload);

        // Strip camera metadata: it carries GPS/device data and adds bytes we never use.
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        Downscale(image, _options.MaxDimension);

        return Render(image);
    }

    public ProcessedPhoto Rotate(byte[] stored, bool clockwise)
    {
        using var image = Image.Load(stored);
        image.Mutate(ctx => ctx.Rotate(clockwise ? RotateMode.Rotate90 : RotateMode.Rotate270));

        // Rotation swaps the edges, so re-apply the bound in case the new long edge exceeds it.
        Downscale(image, _options.MaxDimension);

        return Render(image);
    }

    public byte[]? UpgradeThumbnail(byte[] storedThumbnail, byte[] storedImage)
    {
        var target = _options.ThumbnailDimension;
        if (target <= 0 || storedImage.Length == 0) return null;

        var thumbInfo = Image.Identify(storedThumbnail);
        var imageInfo = Image.Identify(storedImage);
        var currentEdge = Math.Max(thumbInfo.Width, thumbInfo.Height);
        var sourceEdge = Math.Max(imageInfo.Width, imageInfo.Height);

        // Nothing sharper to produce if the thumbnail already matches the target or the source.
        if (currentEdge >= target || currentEdge >= sourceEdge) return null;

        using var image = Image.Load(storedImage);
        Downscale(image, target);
        return Encode(image, Math.Clamp(_options.ThumbnailJpegQuality, 1, 100));
    }

    private ProcessedPhoto Render(Image image)
    {
        var full = Encode(image, Math.Clamp(_options.JpegQuality, 1, 100));

        using var thumbnail = image.Clone(ctx => { });
        Downscale(thumbnail, _options.ThumbnailDimension);
        var thumb = Encode(thumbnail, Math.Clamp(_options.ThumbnailJpegQuality, 1, 100));

        return new ProcessedPhoto(full, thumb, image.Width, image.Height);
    }

    private static void Downscale(Image image, int maxDimension)
    {
        if (maxDimension <= 0) return;
        if (image.Width <= maxDimension && image.Height <= maxDimension) return;

        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(maxDimension, maxDimension)
        }));
    }

    private static byte[] Encode(Image image, int quality)
    {
        using var buffer = new MemoryStream();
        image.Save(buffer, new JpegEncoder { Quality = quality });
        return buffer.ToArray();
    }
}
