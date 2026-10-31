using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Webboard.DataAccess;
using Webboard.DataAccess.Entities;

namespace Webboard.Api.Tests;

[Collection("Database")]
public class TopicCommentSchemaTests(WebboardApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private WebboardDbContext NewDb() => factory.Services.CreateScope().ServiceProvider.GetRequiredService<WebboardDbContext>();

    private static async Task<User> AddUserAsync(WebboardDbContext db)
    {
        var user = new User { Username = "alice", DisplayName = "Alice", PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static Topic NewTopic(int userId, string? title = null) => new()
    {
        UserId = userId,
        Title = title ?? "Hello",
        Body = "World",
        CreatedAt = DateTime.UtcNow,
    };

    private static Comment NewComment(int topicId, int userId, int? parentId = null) => new()
    {
        TopicId = topicId,
        UserId = userId,
        ParentCommentId = parentId,
        Body = "Nice",
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task InsertCommentWithUnknownTopic_ThrowsDbUpdateException()
    {
        var db = NewDb();
        var user = await AddUserAsync(db);
        db.Comments.Add(NewComment(topicId: 999, user.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task InsertCommentWithUnknownParent_ThrowsDbUpdateException()
    {
        var db = NewDb();
        var user = await AddUserAsync(db);
        var topic = NewTopic(user.Id);
        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        db.Comments.Add(NewComment(topic.Id, user.Id, parentId: 999));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task InsertTopicWithUnknownUser_ThrowsDbUpdateException()
    {
        var db = NewDb();
        db.Topics.Add(NewTopic(userId: 999));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteTopic_CascadesToComments()
    {
        var db = NewDb();
        var user = await AddUserAsync(db);
        var topic = NewTopic(user.Id);
        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        var parent = NewComment(topic.Id, user.Id);
        db.Comments.Add(parent);
        await db.SaveChangesAsync();
        db.Comments.Add(NewComment(topic.Id, user.Id, parent.Id));
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM topic");

        Assert.Equal(0, await db.Comments.CountAsync());
    }

    [Fact]
    public async Task DeleteParentComment_CascadesToReplies()
    {
        var db = NewDb();
        var user = await AddUserAsync(db);
        var topic = NewTopic(user.Id);
        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        var parent = NewComment(topic.Id, user.Id);
        db.Comments.Add(parent);
        await db.SaveChangesAsync();
        db.Comments.Add(NewComment(topic.Id, user.Id, parent.Id));
        await db.SaveChangesAsync();

        await db.Database.ExecuteSqlRawAsync("DELETE FROM comment WHERE parent_comment_id IS NULL");

        Assert.Equal(0, await db.Comments.CountAsync());
    }

    [Fact]
    public async Task InsertTopicWithTitleOf201Chars_ThrowsDbUpdateException()
    {
        var db = NewDb();
        var user = await AddUserAsync(db);
        db.Topics.Add(NewTopic(user.Id, new string('a', 201)));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task InsertTopicWithTitleOf200Chars_Succeeds()
    {
        var db = NewDb();
        var user = await AddUserAsync(db);
        db.Topics.Add(NewTopic(user.Id, new string('a', 200)));

        await db.SaveChangesAsync();
    }
}
