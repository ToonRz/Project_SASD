namespace Webboard.DataAccess.Entities;

public class Topic
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User Author { get; set; } = null!;
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Comment> Comments { get; set; } = [];
}
