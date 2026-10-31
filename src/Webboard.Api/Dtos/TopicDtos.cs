using System.ComponentModel.DataAnnotations;

namespace Webboard.Api.Dtos;

public record TopicSummaryDto(int Id, string Title, string AuthorName, DateTime CreatedAt, int CommentCount);

public record TopicDetailDto(int Id, string Title, string Body, string AuthorName, DateTime CreatedAt, IReadOnlyList<CommentDto> Comments);

public record CreateTopicRequest([Required, MaxLength(200)] string Title, [Required] string Body);
