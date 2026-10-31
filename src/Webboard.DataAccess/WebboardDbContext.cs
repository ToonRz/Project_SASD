using Microsoft.EntityFrameworkCore;
using Webboard.DataAccess.Entities;

namespace Webboard.DataAccess;

public class WebboardDbContext(DbContextOptions<WebboardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebboardDbContext).Assembly);
}
