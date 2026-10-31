using Microsoft.EntityFrameworkCore;
using Webboard.Api.Dtos;
using Webboard.Api.Security;
using Webboard.DataAccess;
using Webboard.DataAccess.Entities;

namespace Webboard.Api.Services;

public enum LoginStatus
{
    Success,
    InvalidCredentials,
    LockedOut,
}

public record LoginResult(LoginStatus Status, LoginResponse? Response = null, TimeSpan? RetryAfter = null);

public class AuthService(WebboardDbContext db, TokenService tokens, TimeProvider time, ILogger<AuthService> logger)
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    public static UserDto ToDto(User user) => new(user.Id, user.Username, user.DisplayName, user.TwoFactorEnabled);

    /// <summary>Returns null when the username is already taken.</summary>
    public async Task<UserDto?> RegisterAsync(RegisterRequest request)
    {
        var username = NormalizeUsername(request.Username);
        if (await db.Users.AnyAsync(u => u.Username == username))
        {
            return null;
        }

        var user = new User
        {
            Username = username,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            CreatedAt = time.GetUtcNow(),
        };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return null; // lost a race against the unique index on username
        }

        return ToDto(user);
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var username = NormalizeUsername(request.Username);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == username);
        if (user is null)
        {
            // Spend the same PBKDF2 time as a real check so the response time does not reveal the username.
            PasswordHasher.Verify(request.Password, PasswordHasher.DummyHash);
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var now = time.GetUtcNow();
        if (user.LockoutUntil > now)
        {
            return new LoginResult(LoginStatus.LockedOut, RetryAfter: user.LockoutUntil.Value - now);
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            var retryAfter = await RegisterFailureAsync(user, now);
            return retryAfter is null
                ? new LoginResult(LoginStatus.InvalidCredentials)
                : new LoginResult(LoginStatus.LockedOut, RetryAfter: retryAfter);
        }

        if (user.FailedLoginCount != 0)
        {
            user.FailedLoginCount = 0;
            await db.SaveChangesAsync();
        }

        return new LoginResult(LoginStatus.Success, new LoginResponse(tokens.CreateMfaToken(user), !user.TwoFactorEnabled));
    }

    /// <summary>Counts a failed attempt; returns the lockout length when this attempt triggered one.</summary>
    internal async Task<TimeSpan?> RegisterFailureAsync(User user, DateTimeOffset now)
    {
        user.FailedLoginCount++;
        TimeSpan? lockout = null;
        if (user.FailedLoginCount >= MaxFailedAttempts)
        {
            user.FailedLoginCount = 0;
            user.LockoutUntil = now + LockoutDuration;
            lockout = LockoutDuration;
            logger.LogWarning("User {UserId} locked out until {LockoutUntil:o}", user.Id, user.LockoutUntil);
        }

        await db.SaveChangesAsync();
        return lockout;
    }
}
