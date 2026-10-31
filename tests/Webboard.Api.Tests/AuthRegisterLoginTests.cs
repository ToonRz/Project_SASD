using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Webboard.Api.Tests;

[Collection("Database")]
public class AuthRegisterLoginTests(WebboardApiFactory factory) : IAsyncLifetime
{
    private const string Password = "S3cret-pass-A1";

    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private Task<HttpResponseMessage> RegisterAsync(string username, string password = Password, string displayName = "Ton") =>
        _client.PostAsJsonAsync("/api/auth/register", new { username, displayName, password });

    private Task<HttpResponseMessage> LoginAsync(string username, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new { username, password });

    [Fact]
    public async Task Register_Valid_Returns201WithoutTwoFactor()
    {
        var response = await RegisterAsync("TON");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(text).RootElement;
        Assert.Equal("ton", body.GetProperty("username").GetString());
        Assert.False(body.GetProperty("twoFactorEnabled").GetBoolean());
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_SameUsernameDifferentCase_Returns409()
    {
        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync("TON")).StatusCode);

        var response = await RegisterAsync("ton");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400WithPasswordError()
    {
        var response = await RegisterAsync("ton", "1234567");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Contains(errors.EnumerateObject(), p => p.Name.Equals("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_UsernameWithSpace_Returns400()
    {
        var response = await RegisterAsync("a b");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_Correct_ReturnsMfaToken()
    {
        await RegisterAsync("ton");

        var response = await LoginAsync("Ton", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("twoFactorSetupRequired").GetBoolean());
        var token = new JsonWebTokenHandler().ReadJsonWebToken(body.GetProperty("mfaToken").GetString());
        Assert.Equal("mfa", token.GetClaim("purpose").Value);
        Assert.Equal("Ton", token.GetClaim("name").Value);
        Assert.False(string.IsNullOrEmpty(token.GetClaim("sub").Value));
        var lifetime = token.ValidTo - DateTime.UtcNow;
        Assert.InRange(lifetime, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5.1));
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownUser_AreIndistinguishable()
    {
        await RegisterAsync("ton");

        var wrong = await LoginAsync("ton", "not-the-password");
        var unknown = await LoginAsync("nobody", "not-the-password");

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        var a = await wrong.Content.ReadFromJsonAsync<JsonElement>();
        var b = await unknown.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var field in new[] { "type", "title", "status", "detail" })
        {
            Assert.Equal(a.GetProperty(field).ToString(), b.GetProperty(field).ToString());
        }
    }

    [Fact]
    public async Task Login_FiveWrongPasswords_LocksAccountEvenForCorrectPassword()
    {
        await RegisterAsync("ton");

        for (var i = 1; i <= 4; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("ton", "wrong-password")).StatusCode);
        }

        var fifth = await LoginAsync("ton", "wrong-password");
        Assert.Equal(HttpStatusCode.TooManyRequests, fifth.StatusCode);
        Assert.True(fifth.Headers.RetryAfter?.Delta > TimeSpan.Zero);

        var correctWhileLocked = await LoginAsync("ton", Password);
        Assert.Equal(HttpStatusCode.TooManyRequests, correctWhileLocked.StatusCode);
        Assert.NotNull(correctWhileLocked.Headers.RetryAfter);
    }

    [Fact]
    public async Task Login_SuccessResetsFailureCounter()
    {
        await RegisterAsync("ton");
        for (var i = 0; i < 4; i++)
        {
            await LoginAsync("ton", "wrong-password");
        }

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("ton", Password)).StatusCode);

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("ton", "wrong-password")).StatusCode);
        }
    }

    [Fact]
    public async Task Logs_NeverContainPasswords()
    {
        const string distinctive = "Zq9-distinctive-pw-77";
        await RegisterAsync("ton", distinctive);
        await LoginAsync("ton", distinctive);
        await LoginAsync("ton", distinctive + "x");

        Assert.DoesNotContain(factory.Logs.Entries, e => e.Contains(distinctive));
    }

    [Theory]
    [InlineData("")]
    [InlineData("c2hvcnQ=")]
    public void Startup_WithMissingOrShortSigningKey_FailsNamingTheKey(string key)
    {
        using var broken = factory.WithWebHostBuilder(b => b.UseSetting("Jwt:SigningKey", key));

        var ex = Assert.ThrowsAny<Exception>(() => broken.CreateClient());

        var messages = new List<string>();
        for (var e = ex; e is not null; e = e.InnerException)
        {
            messages.Add(e.Message);
        }

        Assert.Contains(messages, m => m.Contains("Jwt:SigningKey"));
    }
}
