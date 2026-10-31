using Microsoft.EntityFrameworkCore;
using Webboard.Api.Dtos;
using Webboard.DataAccess;
using Webboard.DataAccess.Entities;

namespace Webboard.Api.Services;

public class TopicService(WebboardDbContext db, TimeProvider time)
{
    public async Task<IReadOnlyList<TopicSummaryDto>> ListAsync() =>
        await db.Topics
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .Select(t => new TopicSummaryDto(t.Id, t.Title, t.Author.DisplayName, t.CreatedAt, t.Comments.Count))
            .ToListAsync();

    public Task<TopicDetailDto?> GetAsync(int id) =>
        db.Topics
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TopicDetailDto(
                t.Id,
                t.Title,
                t.Body,
                t.Author.DisplayName,
                t.CreatedAt,
                t.Comments
                    .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
                    .Select(c => new CommentDto(c.Id, c.TopicId, c.ParentCommentId, c.Author.DisplayName, c.Body, c.CreatedAt))
                    .ToList()))
            .SingleOrDefaultAsync();

    /// <summary>Returns null when <paramref name="userId"/> no longer exists.</summary>
    public async Task<TopicSummaryDto?> CreateAsync(int userId, CreateTopicRequest request)
    {
        var authorName = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .SingleOrDefaultAsync();
        if (authorName is null)
        {
            return null;
        }

        var topic = new Topic
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.Topics.Add(topic);
        await db.SaveChangesAsync();

        return new TopicSummaryDto(topic.Id, topic.Title, authorName, topic.CreatedAt, 0);
    }
}
