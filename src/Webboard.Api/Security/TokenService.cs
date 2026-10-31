using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Webboard.DataAccess.Entities;

namespace Webboard.Api.Security;

public class TokenService(IOptions<JwtOptions> options, TimeProvider time)
{
    public const string PurposeClaim = "purpose";
    public const string MfaPurpose = "mfa";

    public static readonly TimeSpan MfaLifetime = TimeSpan.FromMinutes(5);

    public string CreateMfaToken(User user) => Create(user, MfaPurpose, MfaLifetime);

    private string Create(User user, string purpose, TimeSpan lifetime)
    {
        var jwt = options.Value;
        var now = time.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now + lifetime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("name", user.DisplayName),
                new Claim(PurposeClaim, purpose),
            ]),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(jwt.GetKeyBytes()), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
