using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Webboard.Api.Security;

/// <summary>
/// A valid token of the wrong purpose (MFA token on a normal endpoint, access token on <c>/api/auth/2fa/*</c>)
/// fails the policy as "forbidden"; the spec wants <c>401</c> for it.
/// </summary>
public class PurposeMismatchAs401Handler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult) =>
        authorizeResult.Forbidden
            ? context.ChallengeAsync(JwtBearerDefaults.AuthenticationScheme)
            : _default.HandleAsync(next, context, policy, authorizeResult);
}
