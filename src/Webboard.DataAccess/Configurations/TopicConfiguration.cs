using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Webboard.DataAccess.Entities;

namespace Webboard.DataAccess.Configurations;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("topic");
        builder.Property(t => t.Title).HasMaxLength(200);
        builder.Property(t => t.CreatedAt).HasColumnType("timestamptz");
        builder.HasOne(t => t.Author).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(t => t.Comments).WithOne(c => c.Topic).HasForeignKey(c => c.TopicId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => t.CreatedAt);
    }
}
