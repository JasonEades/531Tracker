using System.ComponentModel.DataAnnotations;

namespace FiveThreeOneTracker.Models;

public enum HealthConnectionStatus
{
    Connected,
    Revoked,
    RequiresReconnect
}

public sealed class GoogleHealthConnection
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(255)]
    public string GoogleSubject { get; set; } = string.Empty;

    [Required]
    public string EncryptedAccessToken { get; set; } = string.Empty;

    public string? EncryptedRefreshToken { get; set; }

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    [Required, StringLength(1000)]
    public string GrantedScopes { get; set; } = string.Empty;

    public HealthConnectionStatus Status { get; set; } = HealthConnectionStatus.Connected;
    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? LastSyncError { get; set; }
}
