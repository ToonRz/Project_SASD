using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Webboard.Api.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's id, taken from the token's <c>sub</c> claim (never from a request body).</summary>
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("The principal has no valid 'sub' claim.");
}
