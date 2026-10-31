using Microsoft.AspNetCore.Mvc;
using Webboard.Api.Dtos;
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
                Response.Headers.RetryAfter = Math.Ceiling(result.RetryAfter!.Value.TotalSeconds).ToString("0");
                return Problem(statusCode: StatusCodes.Status429TooManyRequests, detail: "Too many failed attempts. Try again later.");
            default:
                return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid username or password.");
        }
    }
}
