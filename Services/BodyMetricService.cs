using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FiveThreeOneTracker.Services;

public sealed class BodyMetricTrend
{
    public string Label { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public double? Latest { get; init; }
    public DateTime? LatestOn { get; init; }
    public double? ChangeOver30Days { get; init; }
    public double? ChangeOver90Days { get; init; }
}

public sealed class PhotoQuota
{
    public int Used { get; init; }
    public int Limit { get; init; }
    public int Remaining => Math.Max(0, Limit - Used);
    public bool IsFull => Used >= Limit;
}

public sealed record PhotoUploadResult(bool Success, string? Error, ProgressPhoto? Photo);

/// <summary>Photo metadata without the image bytes, for gallery listings.</summary>
public sealed record ProgressPhotoInfo(int Id, DateTime TakenOn, string? Caption, long SizeBytes, DateTime UpdatedAtUtc)
{
    /// <summary>Cache-busting token so a rotated photo is refetched instead of served from cache.</summary>
    public long Version => UpdatedAtUtc.Ticks;
}

public interface IBodyMetricService
{
    Task<List<BodyMetricEntry>> GetEntriesAsync(int take = 180);
    Task<BodyMetricEntry?> GetEntryAsync(DateTime recordedOn);
    Task<BodyMetricEntry> UpsertAsync(BodyMetricEntry entry);
    Task<bool> DeleteEntryAsync(int id);
    Task<List<BodyMetricTrend>> GetTrendsAsync();

    Task<PhotoQuota> GetPhotoQuotaAsync();
    Task<List<ProgressPhotoInfo>> GetPhotosAsync();
    Task<PhotoUploadResult> AddPhotoAsync(Stream upload, long uploadLength, DateTime takenOn, string? caption, CancellationToken ct = default);
    Task<bool> DeletePhotoAsync(int id, CancellationToken ct = default);

    /// <summary>Rotates a stored photo 90 degrees and regenerates its thumbnail.</summary>
    Task<bool> RotatePhotoAsync(int id, bool clockwise, CancellationToken ct = default);

    /// <summary>Sets or clears a photo's caption after it was uploaded.</summary>
    Task<bool> UpdatePhotoCaptionAsync(int id, string? caption, CancellationToken ct = default);

    /// <summary>
    /// Reads photo bytes for an explicit user. The owner is passed in because this is called
    /// from a plain HTTP endpoint, where the Razor component auth state is not available.
    /// </summary>
    Task<(byte[] Content, string ContentType)?> OpenPhotoAsync(int id, bool thumbnail, string userId, CancellationToken ct = default);
}

public sealed class BodyMetricService(
    AppDbContext db,
    ICurrentUserService userContext,
    IProgressPhotoProcessor processor,
    IOptions<ProgressPhotoOptions> options) : IBodyMetricService
{
    private readonly ProgressPhotoOptions _options = options.Value;

    /// <summary>Matches the <see cref="ProgressPhoto.Caption"/> column length.</summary>
    private const int MaxCaptionLength = 200;

    public async Task<List<BodyMetricEntry>> GetEntriesAsync(int take = 180)
    {
        var userId = await userContext.GetUserIdAsync();
        return await db.BodyMetricEntries.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RecordedOn)
            .Take(take)
            .ToListAsync();
    }

    public async Task<BodyMetricEntry?> GetEntryAsync(DateTime recordedOn)
    {
        var userId = await userContext.GetUserIdAsync();
        var date = recordedOn.Date;
        return await db.BodyMetricEntries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RecordedOn == date);
    }

    public async Task<BodyMetricEntry> UpsertAsync(BodyMetricEntry entry)
    {
        var userId = await userContext.GetUserIdAsync();
        var date = entry.RecordedOn.Date;

        var existing = await db.BodyMetricEntries
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RecordedOn == date);

        if (existing is null)
        {
            existing = new BodyMetricEntry { UserId = userId, RecordedOn = date };
            db.BodyMetricEntries.Add(existing);
        }

        existing.WeightLb = entry.WeightLb;
        existing.BodyFatPercent = entry.BodyFatPercent;
        existing.NeckInches = entry.NeckInches;
        existing.ChestInches = entry.ChestInches;
        existing.WaistInches = entry.WaistInches;
        existing.HipsInches = entry.HipsInches;
        existing.ThighInches = entry.ThighInches;
        existing.ArmInches = entry.ArmInches;
        existing.Notes = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes.Trim();
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteEntryAsync(int id)
    {
        var userId = await userContext.GetUserIdAsync();
        var entry = await db.BodyMetricEntries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (entry is null) return false;

        db.BodyMetricEntries.Remove(entry);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<BodyMetricTrend>> GetTrendsAsync()
    {
        var entries = await GetEntriesAsync(400);
        if (entries.Count == 0) return [];

        var trends = new List<BodyMetricTrend>
        {
            BuildTrend(entries, "Bodyweight", "lb", x => x.WeightLb),
            BuildTrend(entries, "Body fat", "%", x => x.BodyFatPercent),
            BuildTrend(entries, "Waist", "in", x => x.WaistInches),
            BuildTrend(entries, "Chest", "in", x => x.ChestInches),
            BuildTrend(entries, "Arm", "in", x => x.ArmInches),
            BuildTrend(entries, "Thigh", "in", x => x.ThighInches)
        };

        return trends.Where(x => x.Latest.HasValue).ToList();
    }

    private static BodyMetricTrend BuildTrend(
        List<BodyMetricEntry> entriesDescending,
        string label,
        string unit,
        Func<BodyMetricEntry, double?> selector)
    {
        var points = entriesDescending
            .Where(x => selector(x).HasValue)
            .Select(x => (x.RecordedOn, Value: selector(x)!.Value))
            .ToList();

        if (points.Count == 0) return new BodyMetricTrend { Label = label, Unit = unit };

        var latest = points[0];

        return new BodyMetricTrend
        {
            Label = label,
            Unit = unit,
            Latest = latest.Value,
            LatestOn = latest.RecordedOn,
            ChangeOver30Days = ChangeSince(points, latest, 30),
            ChangeOver90Days = ChangeSince(points, latest, 90)
        };
    }

    /// <summary>Compares the latest reading with the closest reading at or before the cutoff.</summary>
    private static double? ChangeSince(
        List<(DateTime RecordedOn, double Value)> pointsDescending,
        (DateTime RecordedOn, double Value) latest,
        int days)
    {
        var cutoff = latest.RecordedOn.AddDays(-days);
        var baseline = pointsDescending.FirstOrDefault(x => x.RecordedOn <= cutoff);
        if (baseline == default) return null;
        return latest.Value - baseline.Value;
    }

    public async Task<PhotoQuota> GetPhotoQuotaAsync()
    {
        var userId = await userContext.GetUserIdAsync();
        var used = await db.ProgressPhotos.CountAsync(x => x.UserId == userId);
        return new PhotoQuota { Used = used, Limit = Math.Max(1, _options.MaxPhotosPerUser) };
    }

    public async Task<List<ProgressPhotoInfo>> GetPhotosAsync()
    {
        var userId = await userContext.GetUserIdAsync();

        // Image bytes are deliberately excluded here so listing the gallery stays cheap.
        return await db.ProgressPhotos.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.TakenOn)
            .ThenByDescending(x => x.Id)
            .Select(x => new ProgressPhotoInfo(x.Id, x.TakenOn, x.Caption, x.SizeBytes, x.UpdatedAtUtc))
            .ToListAsync();
    }

    public async Task<PhotoUploadResult> AddPhotoAsync(
        Stream upload,
        long uploadLength,
        DateTime takenOn,
        string? caption,
        CancellationToken ct = default)
    {
        var maxBytes = Math.Max(1, _options.MaxUploadMegabytes) * 1024L * 1024L;
        if (uploadLength > maxBytes)
        {
            return new PhotoUploadResult(false, $"That photo is larger than the {_options.MaxUploadMegabytes} MB upload limit.", null);
        }

        var quota = await GetPhotoQuotaAsync();
        if (quota.IsFull)
        {
            return new PhotoUploadResult(false, $"You've reached the {quota.Limit} photo limit. Delete an older photo to add a new one.", null);
        }

        ProcessedPhoto processed;
        try
        {
            using var buffer = new MemoryStream();
            await upload.CopyToAsync(buffer, ct);
            if (buffer.Length > maxBytes)
            {
                return new PhotoUploadResult(false, $"That photo is larger than the {_options.MaxUploadMegabytes} MB upload limit.", null);
            }

            buffer.Position = 0;
            processed = processor.Process(buffer);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new PhotoUploadResult(false, "That file could not be read as an image.", null);
        }

        var userId = await userContext.GetUserIdAsync();

        var photo = new ProgressPhoto
        {
            UserId = userId,
            TakenOn = takenOn.Date,
            ImageData = processed.Image,
            ThumbnailData = processed.Thumbnail,
            ContentType = "image/jpeg",
            SizeBytes = processed.Image.Length,
            ThumbnailSizeBytes = processed.Thumbnail.Length,
            Width = processed.Width,
            Height = processed.Height,
            Caption = NormalizeCaption(caption)
        };

        db.ProgressPhotos.Add(photo);
        await db.SaveChangesAsync(ct);

        return new PhotoUploadResult(true, null, photo);
    }

    private static string? NormalizeCaption(string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption)) return null;
        var trimmed = caption.Trim();
        return trimmed.Length > MaxCaptionLength ? trimmed[..MaxCaptionLength] : trimmed;
    }

    public async Task<bool> DeletePhotoAsync(int id, CancellationToken ct = default)
    {
        var userId = await userContext.GetUserIdAsync();
        var photo = await db.ProgressPhotos.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (photo is null) return false;

        db.ProgressPhotos.Remove(photo);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RotatePhotoAsync(int id, bool clockwise, CancellationToken ct = default)
    {
        var userId = await userContext.GetUserIdAsync();
        var photo = await db.ProgressPhotos.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (photo is null || photo.ImageData.Length == 0) return false;

        ProcessedPhoto rotated;
        try
        {
            rotated = processor.Rotate(photo.ImageData, clockwise);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }

        photo.ImageData = rotated.Image;
        photo.ThumbnailData = rotated.Thumbnail;
        photo.SizeBytes = rotated.Image.Length;
        photo.ThumbnailSizeBytes = rotated.Thumbnail.Length;
        photo.Width = rotated.Width;
        photo.Height = rotated.Height;

        // Must strictly increase: the clock's granularity is coarser than a fast rotate/reload,
        // and a repeated value would leave the browser showing the pre-rotation image.
        var now = DateTime.UtcNow;
        photo.UpdatedAtUtc = now > photo.UpdatedAtUtc ? now : photo.UpdatedAtUtc.AddTicks(1);

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdatePhotoCaptionAsync(int id, string? caption, CancellationToken ct = default)
    {
        var userId = await userContext.GetUserIdAsync();
        var photo = await db.ProgressPhotos.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (photo is null) return false;

        var trimmed = NormalizeCaption(caption);

        photo.Caption = trimmed;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(byte[] Content, string ContentType)?> OpenPhotoAsync(int id, bool thumbnail, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        var photo = await db.ProgressPhotos.AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(x => new
            {
                Content = thumbnail ? x.ThumbnailData : x.ImageData,
                x.ContentType
            })
            .FirstOrDefaultAsync(ct);

        return photo is null || photo.Content.Length == 0
            ? null
            : (photo.Content, photo.ContentType);
    }
}
