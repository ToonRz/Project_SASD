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

public enum TwoFactorStatus
{
    Success,
    UserNotFound,
    AlreadyEnabled,
    SetupNotStarted,
    InvalidCode,
    LockedOut,
}

public record TwoFactorResult<T>(TwoFactorStatus Status, T? Value = default, TimeSpan? RetryAfter = null);

public class AuthService(WebboardDbContext db, TokenService tokens, TotpService totp, TimeProvider time, ILogger<AuthService> logger)
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

    public async Task<UserDto?> GetUserAsync(int userId) =>
        await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => ToDto(u)).SingleOrDefaultAsync();

    /// <summary>Creates (or, until verified, replaces) the user's shared key and returns it once.</summary>
    public async Task<TwoFactorResult<TwoFactorSetupResponse>> BeginTwoFactorSetupAsync(int userId)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return new(TwoFactorStatus.UserNotFound);
        }

        if (user.TwoFactorEnabled)
        {
            return new(TwoFactorStatus.AlreadyEnabled);
        }

        var sharedKey = TotpService.NewSharedKey();
        user.TotpSecretProtected = totp.Protect(sharedKey);
        await db.SaveChangesAsync();
        return new(TwoFactorStatus.Success, new TwoFactorSetupResponse(sharedKey, TotpService.BuildOtpAuthUri(user.Username, sharedKey)));
    }

    public async Task<TwoFactorResult<AuthResponse>> VerifyTwoFactorAsync(int userId, string code)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return new(TwoFactorStatus.UserNotFound);
        }

        var now = time.GetUtcNow();
        if (user.LockoutUntil > now)
        {
            return new(TwoFactorStatus.LockedOut, RetryAfter: user.LockoutUntil.Value - now);
        }

        if (user.TotpSecretProtected is null)
        {
            return new(TwoFactorStatus.SetupNotStarted);
        }

        var step = totp.Match(totp.Unprotect(user.TotpSecretProtected), code);
        if (step is not null && await TryAcceptStepAsync(user.Id, step.Value))
        {
            await db.Entry(user).ReloadAsync();
            var response = new AuthResponse(tokens.CreateAccessToken(user), (int)TokenService.AccessLifetime.TotalSeconds, ToDto(user));
            return new(TwoFactorStatus.Success, response);
        }

        // Wrong code or a time step that was already used: both count toward the lockout.
        var retryAfter = await RegisterFailureAsync(user, now);
        return retryAfter is null
            ? new(TwoFactorStatus.InvalidCode)
            : new(TwoFactorStatus.LockedOut, RetryAfter: retryAfter);
    }

    // A single conditional UPDATE, so two concurrent requests with the same code cannot both succeed.
    private async Task<bool> TryAcceptStepAsync(int userId, long step) =>
        await db.Users
            .Where(u => u.Id == userId && (u.TotpLastStep == null || u.TotpLastStep < step))
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.TotpLastStep, step)
                .SetProperty(u => u.TwoFactorEnabled, true)
                .SetProperty(u => u.FailedLoginCount, 0)) == 1;

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
