using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Webboard.DataAccess.Entities;

namespace Webboard.DataAccess.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comment");
        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz");
        builder.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.ParentComment).WithMany().HasForeignKey(c => c.ParentCommentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => new { c.TopicId, c.CreatedAt });
    }
}
