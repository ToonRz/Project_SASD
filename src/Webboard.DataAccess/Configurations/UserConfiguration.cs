using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Webboard.DataAccess.Entities;

namespace Webboard.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("app_user");
        builder.Property(u => u.Username).HasMaxLength(30);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.DisplayName).HasMaxLength(50);
        builder.Property(u => u.CreatedAt).HasColumnType("timestamptz");
        builder.Property(u => u.LockoutUntil).HasColumnType("timestamptz");
    }
}
