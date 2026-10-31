using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Webboard.Api.Dtos;

public sealed record RegisterRequest(
    [Required, RegularExpression("^[A-Za-z0-9_]{3,30}$")] string Username,
    [Required, MaxLength(50)] string DisplayName,
    [Required, StringLength(128, MinimumLength = 8)] string Password)
{
    // MVC logs action arguments via ToString at Trace level; keep the password out of it (NFR-006).
    private bool PrintMembers(StringBuilder builder) => builder.Append($"Username = {Username}, DisplayName = {DisplayName}, Password = ***") is not null;
}

public record UserDto(int Id, string Username, string DisplayName, bool TwoFactorEnabled);

public sealed record LoginRequest([Required] string Username, [Required] string Password)
{
    private bool PrintMembers(StringBuilder builder) => builder.Append($"Username = {Username}, Password = ***") is not null;
}

public sealed record LoginResponse(string MfaToken, bool TwoFactorSetupRequired)
{
    private bool PrintMembers(StringBuilder builder) => builder.Append($"MfaToken = ***, TwoFactorSetupRequired = {TwoFactorSetupRequired}") is not null;
}

public sealed record TwoFactorSetupResponse(string SharedKey, string OtpAuthUri)
{
    private bool PrintMembers(StringBuilder builder) => builder.Append("SharedKey = ***, OtpAuthUri = ***") is not null;
}

public sealed record VerifyTwoFactorRequest([Required, RegularExpression("^[0-9]{6}$")] string Code)
{
    private bool PrintMembers(StringBuilder builder) => builder.Append("Code = ***") is not null;
}

public sealed record AuthResponse(string AccessToken, int ExpiresIn, UserDto User)
{
    private bool PrintMembers(StringBuilder builder) => builder.Append($"AccessToken = ***, ExpiresIn = {ExpiresIn}, User = {User}") is not null;
}
