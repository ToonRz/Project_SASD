using System.ComponentModel.DataAnnotations;

namespace Webboard.Api.Dtos;

public record RegisterRequest(
    [Required, RegularExpression("^[A-Za-z0-9_]{3,30}$")] string Username,
    [Required, MaxLength(50)] string DisplayName,
    [Required, StringLength(128, MinimumLength = 8)] string Password);

public record UserDto(int Id, string Username, string DisplayName, bool TwoFactorEnabled);

public record LoginRequest([Required] string Username, [Required] string Password);

public record LoginResponse(string MfaToken, bool TwoFactorSetupRequired);
