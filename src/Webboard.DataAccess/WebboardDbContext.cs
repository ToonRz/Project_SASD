using Microsoft.EntityFrameworkCore;
using Webboard.DataAccess.Entities;

namespace Webboard.DataAccess;

public class WebboardDbContext(DbContextOptions<WebboardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebboardDbContext).Assembly);
}
