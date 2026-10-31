using System.Net.Http.Json;
using System.Text.Json;
using OtpNet;

namespace Webboard.Api.Tests;

/// <summary>Helpers for tests that need a signed-in user (MVP-04/05 reuse <see cref="GetAccessTokenAsync"/>).</summary>
public static class TestAuth
{
    public const string Password = "S3cret-pass-A1";

    /// <summary>
    /// Registers <paramref name="username"/> (a name not used yet in this test), walks the full
    /// login → 2FA setup → 2FA verify flow and returns the access token.
    /// </summary>
    public static async Task<string> GetAccessTokenAsync(HttpClient client, string username)
    {
        var (_, _, accessToken) = await SignInAsync(client, username);
        return accessToken;
    }

    /// <summary>Like <see cref="GetAccessTokenAsync"/> but stops after login and 2FA setup, so the caller can drive verify.</summary>
    public static async Task<(string MfaToken, string SharedKey)> StartTwoFactorSetupAsync(HttpClient client, string username)
    {
        await RegisterAsync(client, username);
        var login = await Json(await client.PostAsJsonAsync("/api/auth/login", new { username, password = Password }));
        var mfaToken = login.GetProperty("mfaToken").GetString()!;

        using var setup = new HttpRequestMessage(HttpMethod.Post, "/api/auth/2fa/setup");
        setup.Headers.Authorization = new("Bearer", mfaToken);
        var setupBody = await Json(await client.SendAsync(setup));
        return (mfaToken, setupBody.GetProperty("sharedKey").GetString()!);
    }

    public static string CurrentCode(string sharedKey, DateTime? at = null) =>
        new Totp(Base32Encoding.ToBytes(sharedKey)).ComputeTotp(at ?? DateTime.UtcNow);

    /// <summary>A six-digit code that is not valid in the previous, current or next time step.</summary>
    public static string WrongCode(string sharedKey)
    {
        var totp = new Totp(Base32Encoding.ToBytes(sharedKey));
        var valid = new[] { -30, 0, 30 }.Select(s => totp.ComputeTotp(DateTime.UtcNow.AddSeconds(s))).ToHashSet();
        return Enumerable.Range(0, 1_000_000).Select(n => n.ToString("D6")).First(c => !valid.Contains(c));
    }

    public static async Task<HttpResponseMessage> VerifyAsync(HttpClient client, string bearerToken, string code)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/2fa/verify")
        {
            Content = JsonContent.Create(new { code }),
        };
        request.Headers.Authorization = new("Bearer", bearerToken);
        return await client.SendAsync(request);
    }

    private static async Task<(string MfaToken, string SharedKey, string AccessToken)> SignInAsync(HttpClient client, string username)
    {
        var (mfaToken, sharedKey) = await StartTwoFactorSetupAsync(client, username);
        var verify = await Json(await VerifyAsync(client, mfaToken, CurrentCode(sharedKey)));
        return (mfaToken, sharedKey, verify.GetProperty("accessToken").GetString()!);
    }

    private static async Task RegisterAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new { username, displayName = username, password = Password });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
