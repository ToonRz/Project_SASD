namespace Webboard.DataAccess.Entities;

public class Comment
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
    public int UserId { get; set; }
    public User Author { get; set; } = null!;

    /// <summary>Reserved for Reply (D13); always null in the MVP.</summary>
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; }
}
