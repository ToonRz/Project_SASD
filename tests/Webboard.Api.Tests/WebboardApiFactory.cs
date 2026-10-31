using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using Webboard.DataAccess;

namespace Webboard.Api.Tests;

public class WebboardApiFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringKey = "ConnectionStrings:WebboardTest";

    // Random per test run (spec-webboard-mvp D12); never read from user-secrets or any file.
    private static readonly string SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private readonly string _connectionString;

    public CapturingLoggerProvider Logs { get; } = new();

    public WebboardApiFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<WebboardApiFactory>()
            .AddEnvironmentVariables()
            .Build();

        _connectionString = ReadTestConnectionString(configuration);
    }

    public static string ReadTestConnectionString(IConfiguration configuration) =>
        configuration[ConnectionStringKey]
        ?? throw new InvalidOperationException(
            $"Missing configuration '{ConnectionStringKey}'. Set it with " +
            $"'dotnet user-secrets set {ConnectionStringKey} <value> --project tests/Webboard.Api.Tests' " +
            "or the environment variable ConnectionStrings__WebboardTest.");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Webboard", _connectionString);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }

    // The factory is a collection fixture, so this runs once per test run.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<WebboardDbContext>().Database.Migrate();
        return host;
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WebboardDbContext>();

        var tables = db.Model.GetEntityTypes()
            .Select(e => (Schema: e.GetSchema() ?? "public", Table: e.GetTableName()))
            .Where(t => t.Table is not null)
            .Select(t => $"\"{t.Schema}\".\"{t.Table}\"")
            .Distinct()
            .ToList();

        if (tables.Count == 0)
        {
            return;
        }

#pragma warning disable EF1002 // identifiers come from the EF model, not user input
        await db.Database.ExecuteSqlRawAsync($"TRUNCATE {string.Join(", ", tables)} RESTART IDENTITY CASCADE");
#pragma warning restore EF1002
    }

}
