using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Webboard.Api.Security;
using Webboard.Api.Services;
using Webboard.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(
        o => o.HasValidKey(),
        "Missing or too short 'Jwt:SigningKey' (base64, at least 32 bytes). Set it with dotnet user-secrets or the environment variable Jwt__SigningKey.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<TotpService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TopicService>();
builder.Services.AddScoped<CommentService>();

var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Webboard");
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
else if (!builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "Missing configuration 'DataProtection:KeysPath' (a folder outside the repo for the key ring). " +
        "Set it with 'dotnet user-secrets set DataProtection:KeysPath <folder> --project src/Webboard.Api' or the environment variable DataProtection__KeysPath.");
}

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwt, time) =>
    {
        bearer.MapInboundClaims = false;
        bearer.TimeProvider = time;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(jwt.Value.GetKeyBytes()),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            // The library validates lifetime against the wall clock; use the injected clock so tests can move time.
            LifetimeValidator = (notBefore, expires, _, p) =>
            {
                var now = time.GetUtcNow().UtcDateTime;
                return expires is not null && (notBefore is null || notBefore <= now + p.ClockSkew) && expires >= now - p.ClockSkew;
            },
        };
    });
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim(TokenService.PurposeClaim, TokenService.AccessPurpose)
        .Build())
    .AddPolicy(AuthPolicies.MfaPending, policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(TokenService.PurposeClaim, TokenService.MfaPurpose));
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, PurposeMismatchAs401Handler>();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
