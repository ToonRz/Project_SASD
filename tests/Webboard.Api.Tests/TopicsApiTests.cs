using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Webboard.DataAccess;
using Webboard.DataAccess.Entities;

namespace Webboard.Api.Tests;

[Collection("Database")]
public class TopicsApiTests(WebboardApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private Task<HttpResponseMessage> PostTopicAsync(object body, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/topics") { Content = JsonContent.Create(body) };
        if (token is not null)
        {
            request.Headers.Authorization = new("Bearer", token);
        }

        return _client.SendAsync(request);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    [Fact]
    public async Task List_WithoutToken_ReturnsTopicsNewestFirstWithCommentCount()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");
        Assert.Equal(HttpStatusCode.Created, (await PostTopicAsync(new { title = "first", body = "b" }, token)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostTopicAsync(new { title = "second", body = "b" }, token)).StatusCode);
        await AddCommentsAsync(topicId: 1, count: 3);

        var response = await _client.GetAsync("/api/topics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = (await Json(response)).EnumerateArray().ToList();
        Assert.Equal(["second", "first"], items.Select(i => i.GetProperty("title").GetString()));
        Assert.Equal([0, 3], items.Select(i => i.GetProperty("commentCount").GetInt32()));
        Assert.Equal("alice", items[0].GetProperty("authorName").GetString());
    }

    [Fact]
    public async Task Get_UnknownTopic_Returns404ProblemJson()
    {
        var response = await _client.GetAsync("/api/topics/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_ExistingTopic_ReturnsDetailWithBodyAndComments()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");
        await PostTopicAsync(new { title = "t", body = "the body" }, token);

        var response = await _client.GetAsync("/api/topics/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await Json(response);
        Assert.Equal("the body", detail.GetProperty("body").GetString());
        Assert.Equal(0, detail.GetProperty("comments").GetArrayLength());
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var response = await PostTopicAsync(new { title = "t", body = "b" }, token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithMfaToken_Returns401()
    {
        var (mfaToken, _) = await TestAuth.StartTwoFactorSetupAsync(_client, "bob");

        var response = await PostTopicAsync(new { title = "t", body = "b" }, mfaToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyTitle_Returns400WithTitleError()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");

        var response = await PostTopicAsync(new { title = "", body = "b" }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await Json(response)).GetProperty("errors");
        Assert.Contains(errors.EnumerateObject(), p => p.Name.Equals("Title", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Post_TitleOf201Chars_Returns400()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");

        var response = await PostTopicAsync(new { title = new string('a', 201), body = "b" }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Valid_Returns201WithLocationAndAuthorFromToken()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");

        var response = await PostTopicAsync(new { title = "hello", body = "world" }, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await Json(response);
        var id = created.GetProperty("id").GetInt32();
        Assert.Equal($"/api/topics/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("alice", created.GetProperty("authorName").GetString());
        Assert.Equal("hello", created.GetProperty("title").GetString());
        Assert.Equal(0, created.GetProperty("commentCount").GetInt32());
    }

    [Fact]
    public async Task Post_WithForgedAuthorNameInBody_IsIgnored()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");

        var response = await PostTopicAsync(new { title = "t", body = "b", authorName = "mallory", userId = 99 }, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("alice", (await Json(response)).GetProperty("authorName").GetString());
    }

    private async Task AddCommentsAsync(int topicId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebboardDbContext>();
        var userId = db.Users.Select(u => u.Id).First();
        for (var i = 0; i < count; i++)
        {
            db.Comments.Add(new Comment { TopicId = topicId, UserId = userId, Body = $"c{i}", CreatedAt = DateTime.UtcNow });
        }

        await db.SaveChangesAsync();
    }
}
