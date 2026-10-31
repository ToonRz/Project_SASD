using Microsoft.EntityFrameworkCore;
using Webboard.Api.Dtos;
using Webboard.DataAccess;
using Webboard.DataAccess.Entities;

namespace Webboard.Api.Services;

public enum CommentStatus
{
    Created,
    TopicNotFound,
    UserNotFound,
}

public record CommentResult(CommentStatus Status, CommentDto? Comment = null);

public class CommentService(WebboardDbContext db, TimeProvider time)
{
    public async Task<CommentResult> CreateAsync(int userId, int topicId, CreateCommentRequest request)
    {
        if (!await db.Topics.AnyAsync(t => t.Id == topicId))
        {
            return new CommentResult(CommentStatus.TopicNotFound);
        }

        var authorName = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .SingleOrDefaultAsync();
        if (authorName is null)
        {
            return new CommentResult(CommentStatus.UserNotFound);
        }

        var comment = new Comment
        {
            TopicId = topicId,
            UserId = userId,
            ParentCommentId = null, // Reply is out of MVP scope (spec D13)
            Body = request.Body.Trim(),
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return new CommentResult(
            CommentStatus.Created,
            new CommentDto(comment.Id, comment.TopicId, comment.ParentCommentId, authorName, comment.Body, comment.CreatedAt));
    }
}
