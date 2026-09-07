using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FiveThreeOneTracker.Data;
using FiveThreeOneTracker.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FiveThreeOneTracker.Services;

public sealed class GoogleHealthOptions
{
    public const string SectionName = "Authentication:GoogleHealth";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/health/google/callback";
    public string AuthorizationEndpoint { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth";
    public string TokenEndpoint { get; set; } = "https://oauth2.googleapis.com/token";
    public string UserInfoEndpoint { get; set; } = "https://openidconnect.googleapis.com/v1/userinfo";
    public string Scope { get; set; } = "openid email https://www.googleapis.com/auth/googlehealth.activity_and_fitness.readonly";
    public int SyncHourUtc { get; set; } = 2;
}

public sealed record GoogleHealthOAuthState(string UserId, string ReturnUrl, DateTime CreatedAtUtc);

public interface IGoogleHealthAuthorizationService
{
    string CreateAuthorizationUrl(HttpContext context, string userId, string returnUrl);
    Task<GoogleHealthConnection> CompleteAsync(HttpContext context, string code, string state);
    Task DisconnectAsync(string userId);
}

public sealed class GoogleHealthAuthorizationService(
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<GoogleHealthOptions> options,
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    ILogger<GoogleHealthAuthorizationService> logger) : IGoogleHealthAuthorizationService
{
    private const string StatePurpose = "FiveThreeOneTracker.GoogleHealth.OAuthState.v1";
    private readonly GoogleHealthOptions settings = options.Value;
    private readonly IDataProtector stateProtector = dataProtectionProvider.CreateProtector(StatePurpose);
    private readonly IDataProtector tokenProtector = dataProtectionProvider.CreateProtector("FiveThreeOneTracker.GoogleHealth.Tokens.v1");

    public string CreateAuthorizationUrl(HttpContext context, string userId, string returnUrl)
    {
        var state = stateProtector.Protect(System.Text.Json.JsonSerializer.Serialize(
            new GoogleHealthOAuthState(userId, NormalizeReturnUrl(returnUrl), DateTime.UtcNow)));
        var callback = BuildCallbackUrl(context);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = callback,
            ["response_type"] = "code",
            ["scope"] = settings.Scope,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = state
        };
        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(settings.AuthorizationEndpoint, query);
    }

    public async Task<GoogleHealthConnection> CompleteAsync(HttpContext context, string code, string state)
    {
        logger.LogInformation("Google Health OAuth callback started.");
        GoogleHealthOAuthState oauthState;
        try
        {
            oauthState = System.Text.Json.JsonSerializer.Deserialize<GoogleHealthOAuthState>(stateProtector.Unprotect(state))
                ?? throw new InvalidOperationException("OAuth state was empty.");
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or System.Text.Json.JsonException)
        {
            throw new InvalidOperationException("The Google Health authorization state is invalid or expired.", ex);
        }

        if (oauthState.CreatedAtUtc < DateTime.UtcNow.AddMinutes(-10))
            throw new InvalidOperationException("The Google Health authorization request expired.");

        var currentUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId is null || !string.Equals(currentUserId, oauthState.UserId, StringComparison.Ordinal))
            throw new InvalidOperationException("The Google Health authorization does not belong to the signed-in user.");

        var client = httpClientFactory.CreateClient("GoogleHealth");
        using var tokenResponse = await client.PostAsync(settings.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.ClientSecret,
                ["redirect_uri"] = BuildCallbackUrl(context),
                ["grant_type"] = "authorization_code"
            }));
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var providerError = await ReadProviderErrorAsync(tokenResponse);
            logger.LogWarning("Google Health token exchange failed. StatusCode={StatusCode}, ProviderError={ProviderError}",
                (int)tokenResponse.StatusCode, providerError);
            throw new InvalidOperationException($"Google token exchange failed with HTTP {(int)tokenResponse.StatusCode}: {providerError}");
        }
        var tokens = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>()
            ?? throw new InvalidOperationException("Google returned an empty token response.");

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(settings.UserInfoEndpoint, "access_token", tokens.AccessToken));
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var userInfoResponse = await client.SendAsync(userInfoRequest);
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            var providerError = await ReadProviderErrorAsync(userInfoResponse);
            logger.LogWarning("Google Health profile lookup failed. StatusCode={StatusCode}, ProviderError={ProviderError}",
                (int)userInfoResponse.StatusCode, providerError);
            throw new InvalidOperationException($"Google profile lookup failed with HTTP {(int)userInfoResponse.StatusCode}: {providerError}");
        }
        var profile = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfo>()
            ?? throw new InvalidOperationException("Google returned an empty profile response.");

        var user = await userManager.FindByIdAsync(oauthState.UserId)
            ?? throw new InvalidOperationException("The authenticated application user no longer exists.");
        var login = (await userManager.GetLoginsAsync(user)).SingleOrDefault(x => x.LoginProvider == "Google");
        logger.LogInformation(
            "Google Health identity comparison. ApplicationEmail={ApplicationEmail}, HealthEmail={HealthEmail}, " +
            "ApplicationSubjectFingerprint={ApplicationSubjectFingerprint}, HealthSubjectFingerprint={HealthSubjectFingerprint}, " +
            "HealthClientIdSuffix={HealthClientIdSuffix}",
            user.Email,
            profile.Email,
            login is null ? "(no Google login)" : Fingerprint(login.ProviderKey),
            Fingerprint(profile.Subject),
            ClientIdSuffix(settings.ClientId));
        if (login is null || !string.Equals(login.ProviderKey, profile.Subject, StringComparison.Ordinal))
            throw new InvalidOperationException("The authorized Google account does not match the signed-in application account.");

        var connection = await db.GoogleHealthConnections.FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (connection is null)
        {
            connection = new GoogleHealthConnection { UserId = user.Id };
            db.GoogleHealthConnections.Add(connection);
        }

        connection.GoogleSubject = profile.Subject;
        connection.EncryptedAccessToken = tokenProtector.Protect(tokens.AccessToken);
        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
            connection.EncryptedRefreshToken = tokenProtector.Protect(tokens.RefreshToken);
        connection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(tokens.ExpiresIn, 60));
        connection.GrantedScopes = settings.Scope;
        connection.Status = HealthConnectionStatus.Connected;
        connection.ConnectedAtUtc = DateTime.UtcNow;
        connection.RevokedAtUtc = null;
        connection.LastSyncError = null;
        await db.SaveChangesAsync();
        logger.LogInformation("Google Health OAuth connection saved for application user.");
        return connection;
    }

    private static async Task<string> ReadProviderErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return response.ReasonPhrase ?? "No response details were returned.";

        return body.Length <= 500 ? body : body[..500];
    }

    private static string Fingerprint(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? "(missing)"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..12];

    private static string ClientIdSuffix(string value)
        => string.IsNullOrWhiteSpace(value) ? "(missing)" : value[^Math.Min(value.Length, 12)..];

    public async Task DisconnectAsync(string userId)
    {
        var connection = await db.GoogleHealthConnections.FirstOrDefaultAsync(x => x.UserId == userId);
        if (connection is null)
            return;
        connection.Status = HealthConnectionStatus.Revoked;
        connection.RevokedAtUtc = DateTime.UtcNow;
        connection.EncryptedAccessToken = string.Empty;
        connection.EncryptedRefreshToken = null;
        await db.SaveChangesAsync();
    }

    private string BuildCallbackUrl(HttpContext context)
        => $"{context.Request.Scheme}://{context.Request.Host}{settings.CallbackPath}";

    private static string NormalizeReturnUrl(string returnUrl)
        => Uri.TryCreate(returnUrl, UriKind.Relative, out _) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
            ? returnUrl : "/settings";

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);

    private sealed record GoogleUserInfo(
        [property: JsonPropertyName("sub")] string Subject,
        [property: JsonPropertyName("email")] string? Email);
}

public interface IGoogleHealthApiClient
{
    Task<string> GetDailyStepsPayloadAsync(GoogleHealthConnection connection, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

public sealed class GoogleHealthApiClient(
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<GoogleHealthOptions> options,
    AppDbContext db) : IGoogleHealthApiClient
{
    public async Task<string> GetDailyStepsPayloadAsync(GoogleHealthConnection connection, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var protector = dataProtectionProvider.CreateProtector("FiveThreeOneTracker.GoogleHealth.Tokens.v1");
        var token = protector.Unprotect(connection.EncryptedAccessToken);
        var client = httpClientFactory.CreateClient("GoogleHealth");
        if (connection.AccessTokenExpiresAtUtc <= DateTime.UtcNow.AddMinutes(1))
        {
            if (string.IsNullOrWhiteSpace(connection.EncryptedRefreshToken))
                throw new InvalidOperationException("Google Health authorization requires reconnecting.");

            var refreshToken = protector.Unprotect(connection.EncryptedRefreshToken);
            using var tokenResponse = await client.PostAsync(options.Value.TokenEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = options.Value.ClientId,
                    ["client_secret"] = options.Value.ClientSecret,
                    ["refresh_token"] = refreshToken,
                    ["grant_type"] = "refresh_token"
                }), cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();
            var refreshed = await tokenResponse.Content.ReadFromJsonAsync<RefreshedToken>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Google returned an empty refresh response.");
            token = refreshed.AccessToken;
            connection.EncryptedAccessToken = protector.Protect(token);
            connection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(refreshed.ExpiresIn, 60));
            await db.SaveChangesAsync(cancellationToken);
        }

        var endpoint = "https://health.googleapis.com/v4/users/me/dataTypes/steps/dataPoints:dailyRollUp";
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(endpoint, new Dictionary<string, string?>
        {
            ["startTime"] = FormatGoogleTimestamp(startDate.Date),
            ["endTime"] = FormatGoogleTimestamp(endDate.Date.AddDays(1))
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var providerError = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(providerError))
                providerError = response.ReasonPhrase ?? "No response details were returned.";
            throw new InvalidOperationException(
                $"Google Health daily steps request failed with HTTP {(int)response.StatusCode}: {providerError}");
        }
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string FormatGoogleTimestamp(DateTime date)
        => new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc))
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private sealed record RefreshedToken(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
