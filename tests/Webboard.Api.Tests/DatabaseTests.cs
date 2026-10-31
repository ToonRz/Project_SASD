using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Webboard.DataAccess;

namespace Webboard.Api.Tests;

[Collection("Database")]
public class DatabaseTests(WebboardApiFactory factory)
{
    [Fact]
    public async Task DbContext_CanConnectToTestDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebboardDbContext>();

        Assert.True(await db.Database.CanConnectAsync());
    }

    [Fact]
    public void Factory_WithoutTestConnectionString_ThrowsNamingTheKey()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => WebboardApiFactory.ReadTestConnectionString(new ConfigurationBuilder().Build()));

        Assert.Contains("ConnectionStrings:WebboardTest", ex.Message);
    }
}
