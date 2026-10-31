using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Webboard.DataAccess;

namespace Webboard.Api.Tests;

[Collection("Database")]
public class AuthTwoFactorTests(WebboardApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Setup_WithMfaToken_ReturnsSharedKeyAndUri_AndStoresItProtected()
    {
        var (mfaToken, sharedKey) = await TestAuth.StartTwoFactorSetupAsync(_client, "ton");

        using var again = Authed(HttpMethod.Post, "/api/auth/2fa/setup", mfaToken);
        var body = await (await _client.SendAsync(again)).Content.ReadFromJsonAsync<JsonElement>();
        var newKey = body.GetProperty("sharedKey").GetString()!;
        Assert.Equal(
            $"otpauth://totp/Webboard:ton?secret={newKey}&issuer=Webboard&algorithm=SHA1&digits=6&period=30",
            body.GetProperty("otpAuthUri").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebboardDbContext>();
        var stored = (await db.Users.AsNoTracking().SingleAsync()).TotpSecretProtected;
        Assert.False(string.IsNullOrEmpty(stored));
        Assert.DoesNotContain(newKey, stored);
        Assert.DoesNotContain(sharedKey, stored);
    }

    [Fact]
    public async Task Setup_AfterTwoFactorEnabled_Returns409()
    {
        var (mfaToken, sharedKey) = await TestAuth.StartTwoFactorSetupAsync(_client, "ton");
        Assert.Equal(HttpStatusCode.OK, (await TestAuth.VerifyAsync(_client, mfaToken, TestAuth.CurrentCode(sharedKey))).StatusCode);

        using var request = Authed(HttpMethod.Post, "/api/auth/2fa/setup", mfaToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Verify_CorrectCode_ReturnsAccessTokenAndEnablesTwoFactor()
    {
        var (mfaToken, sharedKey) = await TestAuth.StartTwoFactorSetupAsync(_client, "ton");

        var response = await TestAuth.VerifyAsync(_client, mfaToken, TestAuth.CurrentCode(sharedKey));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("accessToken").GetString());
        Assert.Equal("access", token.GetClaim("purpose").Value);
        Assert.Equal(3600, body.GetProperty("expiresIn").GetInt32());
        Assert.InRange(token.ValidTo - DateTime.UtcNow, TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(60.1));
        Assert.Equal("ton", body.GetProperty("user").GetProperty("username").GetString());
        Assert.True(body.GetProperty("user").GetProperty("twoFactorEnabled").GetBoolean());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebboardDbContext>();
        Assert.True((await db.Users.AsNoTracking().SingleAsync()).TwoFactorEnabled);
    }

    [Fact]
    public async Task Verify_SameCodeTwice_SecondIsRejected()
    {
        var (mfaToken, sharedKey) = await TestAuth.StartTwoFactorSetupAsync(_client, "ton");
        var code = TestAuth.CurrentCode(sharedKey);

        Assert.Equal(HttpStatusCode.OK, (await TestAuth.VerifyAsync(_client, mfaToken, code)).StatusCode);
        var replay = await TestAuth.VerifyAsync(_client, mfaToken, code);

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Verify_WrongCodeFiveTimes_LocksOutEvenForCorrectCode()
    {
        var (mfaToken, sharedKey) = await TestAuth.StartTwoFactorSetupAsync(_client, "ton");
        var wrong = TestAuth.WrongCode(sharedKey);

        for (var i = 1; i <= 4; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await TestAuth.VerifyAsync(_client, mfaToken, wrong)).StatusCode);
        }

        var fifth = await TestAuth.VerifyAsync(_client, mfaToken, wrong);
        Assert.Equal(HttpStatusCode.TooManyRequests, fifth.StatusCode);
        Assert.NotNull(fifth.Headers.RetryAfter);

        var correctWhileLocked = await TestAuth.VerifyAsync(_client, mfaToken, TestAuth.CurrentCode(sharedKey));
        Assert.Equal(HttpStatusCode.TooManyRequests, correctWhileLocked.StatusCode);
    }

    [Fact]
    public async Task Verify_MalformedCode_Returns400()
    {
        var (mfaToken, _) = await TestAuth.StartTwoFactorSetupAsync(_client, "ton");

        var response = await TestAuth.VerifyAsync(_client, mfaToken, "12ab56");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Verify_BeforeSetup_Returns400()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { username = "ton", displayName = "Ton", password = TestAuth.Password });
        var login = await (await _client.PostAsJsonAsync("/api/auth/login", new { username = "ton", password = TestAuth.Password }))
            .Content.ReadFromJsonAsync<JsonElement>();

        var response = await TestAuth.VerifyAsync(_client, login.GetProperty("mfaToken").GetString()!, "123456");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_AfterTwoFactorEnabled_DoesNotRequireSetup()
    {
        await TestAuth.GetAccessTokenAsync(_client, "ton");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "ton", password = TestAuth.Password });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("twoFactorSetupRequired").GetBoolean());
    }

    [Fact]
    public async Task Me_WithAccessToken_ReturnsCurrentUser()
    {
        var accessToken = await TestAuth.GetAccessTokenAsync(_client, "ton");

        using var request = Authed(HttpMethod.Get, "/api/auth/me", accessToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ton", body.GetProperty("username").GetString());
        Assert.True(body.GetProperty("twoFactorEnabled").GetBoolean());
    }

    [Fact]
    public async Task Me_WithMfaTokenOrNoToken_Returns401()
    {
        var (mfaToken, _) = await TestAuth.StartTwoFactorSetupAsync(_client, "ton");

        using var withMfa = Authed(HttpMethod.Get, "/api/auth/me", mfaToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(withMfa)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task TwoFactorEndpoints_WithAccessToken_Return401()
    {
        var accessToken = await TestAuth.GetAccessTokenAsync(_client, "ton");

        Assert.Equal(HttpStatusCode.Unauthorized, (await TestAuth.VerifyAsync(_client, accessToken, "123456")).StatusCode);
        using var setup = Authed(HttpMethod.Post, "/api/auth/2fa/setup", accessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(setup)).StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithAnotherKey_Returns401()
    {
        var accessToken = await TestAuth.GetAccessTokenAsync(_client, "ton");
        var tampered = accessToken[..^4] + (accessToken.EndsWith("AAAA") ? "BBBB" : "AAAA");

        using var request = Authed(HttpMethod.Get, "/api/auth/me", tampered);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task ExpiredMfaToken_Returns401()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        using var timed = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<TimeProvider>();
            s.AddSingleton<TimeProvider>(clock);
        }));
        using var client = timed.CreateClient();
        var (mfaToken, sharedKey) = await TestAuth.StartTwoFactorSetupAsync(client, "ton");

        clock.Advance(TimeSpan.FromMinutes(6));
        var response = await TestAuth.VerifyAsync(client, mfaToken, TestAuth.CurrentCode(sharedKey, clock.GetUtcNow().UtcDateTime));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void Startup_WithoutDataProtectionKeysPath_OutsideTesting_FailsNamingTheKey()
    {
        using var production = factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));

        var ex = Assert.ThrowsAny<Exception>(() => production.CreateClient());

        var messages = new List<string>();
        for (var e = ex; e is not null; e = e.InnerException)
        {
            messages.Add(e.Message);
        }

        Assert.Contains(messages, m => m.Contains("DataProtection:KeysPath"));
    }
}
