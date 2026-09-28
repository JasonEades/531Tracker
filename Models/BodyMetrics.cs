using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

/// <summary>A single day's bodyweight and tape measurements. One entry per user per day.</summary>
public sealed class BodyMetricEntry
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public DateTime RecordedOn { get; set; }

    public double? WeightLb { get; set; }
    public double? BodyFatPercent { get; set; }
    public double? NeckInches { get; set; }
    public double? ChestInches { get; set; }
    public double? WaistInches { get; set; }
    public double? HipsInches { get; set; }
    public double? ThighInches { get; set; }
    public double? ArmInches { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool HasAnyValue =>
        WeightLb.HasValue || BodyFatPercent.HasValue || NeckInches.HasValue || ChestInches.HasValue
        || WaistInches.HasValue || HipsInches.HasValue || ThighInches.HasValue || ArmInches.HasValue
        || !string.IsNullOrWhiteSpace(Notes);
}

/// <summary>
/// A progress photo, stored in the database. The original upload is never kept — only a
/// resized, re-encoded JPEG plus a small thumbnail, and the number of photos per user is
/// capped, so the row size and per-user footprint stay bounded.
/// </summary>
public sealed class ProgressPhoto
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public DateTime TakenOn { get; set; }

    /// <summary>Compressed JPEG bytes. Never select this column when listing photos.</summary>
    public byte[] ImageData { get; set; } = [];

    /// <summary>Compressed JPEG bytes for the gallery thumbnail.</summary>
    public byte[] ThumbnailData { get; set; } = [];

    [Required, StringLength(100)]
    public string ContentType { get; set; } = "image/jpeg";

    public long SizeBytes { get; set; }
    public long ThumbnailSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    [StringLength(200)]
    public string? Caption { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Bumped whenever the bytes change (e.g. rotation) so cached URLs are invalidated.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
