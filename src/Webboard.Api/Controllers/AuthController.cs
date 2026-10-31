using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Webboard.Api.Dtos;
using Webboard.Api.Security;
using Webboard.Api.Services;

namespace Webboard.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request)
    {
        var user = await auth.RegisterAsync(request);
        return user is null
            ? Problem(statusCode: StatusCodes.Status409Conflict, detail: "Username is already taken.")
            : StatusCode(StatusCodes.Status201Created, user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await auth.LoginAsync(request);
        switch (result.Status)
        {
            case LoginStatus.Success:
                return result.Response!;
            case LoginStatus.LockedOut:
                return LockedOut(result.RetryAfter!.Value);
            default:
                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid username or password.");
        }
    }

    [HttpPost("2fa/setup")]
    [Authorize(Policy = AuthPolicies.MfaPending)]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor()
    {
        var result = await auth.BeginTwoFactorSetupAsync(User.GetUserId());
        return result.Status switch
        {
            TwoFactorStatus.Success => result.Value!,
            TwoFactorStatus.AlreadyEnabled => Problem(statusCode: StatusCodes.Status409Conflict, detail: "Two-factor authentication is already enabled."),
            _ => Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid token."),
        };
    }

    [HttpPost("2fa/verify")]
    [Authorize(Policy = AuthPolicies.MfaPending)]
    public async Task<ActionResult<AuthResponse>> VerifyTwoFactor(VerifyTwoFactorRequest request)
    {
        var result = await auth.VerifyTwoFactorAsync(User.GetUserId(), request.Code);
        return result.Status switch
        {
            TwoFactorStatus.Success => result.Value!,
            TwoFactorStatus.LockedOut => LockedOut(result.RetryAfter!.Value),
            TwoFactorStatus.SetupNotStarted => Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Two-factor setup has not been started."),
            TwoFactorStatus.InvalidCode => Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid or already used code."),
            _ => Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid token."),
        };
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await auth.GetUserAsync(User.GetUserId());
        return user is null ? Unauthorized() : user;
    }

    private ObjectResult LockedOut(TimeSpan retryAfter)
    {
        Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");
        return Problem(statusCode: StatusCodes.Status429TooManyRequests, detail: "Too many failed attempts. Try again later.");
    }
}
