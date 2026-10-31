namespace Webboard.Api.Dtos;

public record CommentDto(int Id, int TopicId, int? ParentCommentId, string AuthorName, string Body, DateTime CreatedAt);
