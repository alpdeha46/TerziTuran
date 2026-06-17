using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TerziTuran.Web.Data;

namespace TerziTuran.Web.Services;

public class FirebasePushOptions
{
    public string? ProjectId { get; set; }
    public string? ClientEmail { get; set; }
    public string? PrivateKey { get; set; }
    public string? ServiceAccountPath { get; set; }
}

public interface IPushNotificationSender
{
    Task SendToUsersAsync(IEnumerable<int> userIds, string title, string message, string type, int? orderId = null);
}

public class FirebasePushService(
    AppDbContext context,
    IHttpClientFactory httpClientFactory,
    IOptions<FirebasePushOptions> options) : IPushNotificationSender
{
    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static string? _accessToken;
    private static DateTimeOffset _accessTokenExpiresAt;

    private readonly FirebasePushOptions _options = options.Value;

    public async Task SendToUsersAsync(IEnumerable<int> userIds, string title, string message, string type, int? orderId = null)
    {
        var credential = LoadCredential();
        if (credential is null)
        {
            return;
        }

        var tokens = await context.UserPushTokens
            .Where(x => x.IsActive && userIds.Contains(x.UserId))
            .Select(x => new { x.Id, x.Token })
            .Distinct()
            .ToListAsync();

        if (tokens.Count == 0)
        {
            return;
        }

        var accessToken = await GetAccessTokenAsync(credential);
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        foreach (var pushToken in tokens)
        {
            using var response = await client.PostAsync(
                $"https://fcm.googleapis.com/v1/projects/{credential.ProjectId}/messages:send",
                BuildPayload(pushToken.Token, title, message, type, orderId));

            if (response.IsSuccessStatusCode)
            {
                continue;
            }

            var body = await response.Content.ReadAsStringAsync();
            if (body.Contains("UNREGISTERED", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("registration-token-not-registered", StringComparison.OrdinalIgnoreCase) ||
                body.Contains("INVALID_ARGUMENT", StringComparison.OrdinalIgnoreCase))
            {
                var tokenEntity = await context.UserPushTokens.FindAsync(pushToken.Id);
                if (tokenEntity is not null)
                {
                    tokenEntity.IsActive = false;
                    tokenEntity.LastSeenAt = DateTime.UtcNow;
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private StringContent BuildPayload(string token, string title, string message, string type, int? orderId)
    {
        var payload = new
        {
            message = new
            {
                token,
                notification = new
                {
                    title,
                    body = message
                },
                data = new Dictionary<string, string>
                {
                    ["type"] = type,
                    ["title"] = title,
                    ["message"] = message,
                    ["orderId"] = orderId?.ToString() ?? string.Empty
                },
                android = new
                {
                    priority = "high",
                    notification = new
                    {
                        channel_id = "terzi_turan_orders",
                        sound = "default",
                        default_sound = true
                    }
                },
                apns = new
                {
                    headers = new Dictionary<string, string>
                    {
                        ["apns-priority"] = "10"
                    },
                    payload = new
                    {
                        aps = new
                        {
                            sound = "default",
                            contentAvailable = 1
                        }
                    }
                }
            }
        };

        return new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
    }

    private async Task<string> GetAccessTokenAsync(FirebaseServiceAccount credential)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) &&
            _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return _accessToken;
        }

        await TokenLock.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) &&
                _accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return _accessToken;
            }

            using var rsa = RSA.Create();
            rsa.ImportFromPem(credential.PrivateKey);
            var key = new RsaSecurityKey(rsa);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
            var now = DateTimeOffset.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: credential.ClientEmail,
                audience: "https://oauth2.googleapis.com/token",
                claims:
                [
                    new("scope", "https://www.googleapis.com/auth/firebase.messaging")
                ],
                notBefore: now.UtcDateTime,
                expires: now.AddHours(1).UtcDateTime,
                signingCredentials: credentials);

            var assertion = new JwtSecurityTokenHandler().WriteToken(jwt);
            var client = httpClientFactory.CreateClient();
            using var response = await client.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = assertion
                }));

            response.EnsureSuccessStatusCode();
            var raw = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(raw);
            _accessToken = json.RootElement.GetProperty("access_token").GetString();
            var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            return _accessToken ?? throw new InvalidOperationException("FCM access token alinamadi.");
        }
        finally
        {
            TokenLock.Release();
        }
    }

    private FirebaseServiceAccount? LoadCredential()
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountPath) &&
            File.Exists(_options.ServiceAccountPath))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(_options.ServiceAccountPath));
            return new FirebaseServiceAccount(
                ProjectId: json.RootElement.GetProperty("project_id").GetString() ?? string.Empty,
                ClientEmail: json.RootElement.GetProperty("client_email").GetString() ?? string.Empty,
                PrivateKey: NormalizePrivateKey(json.RootElement.GetProperty("private_key").GetString() ?? string.Empty));
        }

        if (string.IsNullOrWhiteSpace(_options.ProjectId) ||
            string.IsNullOrWhiteSpace(_options.ClientEmail) ||
            string.IsNullOrWhiteSpace(_options.PrivateKey))
        {
            return null;
        }

        return new FirebaseServiceAccount(
            ProjectId: _options.ProjectId,
            ClientEmail: _options.ClientEmail,
            PrivateKey: NormalizePrivateKey(_options.PrivateKey));
    }

    private static string NormalizePrivateKey(string value)
        => value.Replace("\\n", "\n", StringComparison.Ordinal);

    private sealed record FirebaseServiceAccount(string ProjectId, string ClientEmail, string PrivateKey);
}
