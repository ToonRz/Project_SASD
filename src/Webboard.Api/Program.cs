using Microsoft.EntityFrameworkCore;
using Webboard.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<WebboardDbContext>((sp, options) =>
{
    var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("Webboard")
        ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:Webboard' (set it with dotnet user-secrets).");
    options.UseNpgsql(cs).UseSnakeCaseNamingConvention();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Webboard API v1"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
