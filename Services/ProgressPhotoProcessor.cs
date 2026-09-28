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
