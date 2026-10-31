using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Webboard.DataAccess;
using Webboard.DataAccess.Entities;

namespace Webboard.Api.Tests;

[Collection("Database")]
public class UserSchemaTests(WebboardApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InsertTwoUsersWithSameUsername_ThrowsDbUpdateException()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebboardDbContext>();
        db.Users.Add(NewUser("alice"));
        await db.SaveChangesAsync();

        db.Users.Add(NewUser("alice"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task NewUser_HasDefaults()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebboardDbContext>();
        db.Users.Add(NewUser("bob"));
        await db.SaveChangesAsync();

        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Username == "bob");

        Assert.False(user.TwoFactorEnabled);
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.TotpSecretProtected);
        Assert.Null(user.LockoutUntil);
        Assert.Equal(TimeSpan.Zero, user.CreatedAt.Offset);
    }

    private static User NewUser(string username) => new()
    {
        Username = username,
        DisplayName = username,
        PasswordHash = "x",
    };
}
