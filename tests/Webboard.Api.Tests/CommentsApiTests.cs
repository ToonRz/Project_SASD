using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Webboard.Api.Tests;

[Collection("Database")]
public class CommentsApiTests(WebboardApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private Task<HttpResponseMessage> PostAsync(string url, object body, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (token is not null)
        {
            request.Headers.Authorization = new("Bearer", token);
        }

        return _client.SendAsync(request);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<int> CreateTopicAsync(string token)
    {
        var response = await PostAsync("/api/topics", new { title = "t", body = "b" }, token);
        return (await Json(response)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");
        var topicId = await CreateTopicAsync(token);

        var response = await PostAsync($"/api/topics/{topicId}/comments", new { body = "hi" }, token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownTopic_Returns404()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");

        var response = await PostAsync("/api/topics/999/comments", new { body = "hi" }, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_EmptyBody_Returns400WithBodyError()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");
        var topicId = await CreateTopicAsync(token);

        var response = await PostAsync($"/api/topics/{topicId}/comments", new { body = "" }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await Json(response)).GetProperty("errors");
        Assert.Contains(errors.EnumerateObject(), p => p.Name.Equals("Body", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Post_Valid_Returns201WithAuthorFromTokenAndNullParent()
    {
        var aliceToken = await TestAuth.GetAccessTokenAsync(_client, "alice");
        var topicId = await CreateTopicAsync(aliceToken);
        var bobToken = await TestAuth.GetAccessTokenAsync(_client, "bob");

        var response = await PostAsync(
            $"/api/topics/{topicId}/comments",
            new { body = "hi", authorName = "mallory", parentCommentId = 1 },
            bobToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comment = await Json(response);
        Assert.Equal("bob", comment.GetProperty("authorName").GetString());
        Assert.Equal(JsonValueKind.Null, comment.GetProperty("parentCommentId").ValueKind);
        Assert.Equal(topicId, comment.GetProperty("topicId").GetInt32());
        Assert.Equal("hi", comment.GetProperty("body").GetString());
        Assert.False(string.IsNullOrEmpty(response.Headers.Location?.OriginalString));
    }

    [Fact]
    public async Task TwoComments_AreReturnedOldestFirstAndCounted()
    {
        var token = await TestAuth.GetAccessTokenAsync(_client, "alice");
        var topicId = await CreateTopicAsync(token);
        await PostAsync($"/api/topics/{topicId}/comments", new { body = "first" }, token);
        await PostAsync($"/api/topics/{topicId}/comments", new { body = "second" }, token);

        var detail = await Json(await _client.GetAsync($"/api/topics/{topicId}"));
        var list = await Json(await _client.GetAsync("/api/topics"));

        Assert.Equal(["first", "second"], detail.GetProperty("comments").EnumerateArray().Select(c => c.GetProperty("body").GetString()));
        Assert.Equal(2, list[0].GetProperty("commentCount").GetInt32());
    }
}
