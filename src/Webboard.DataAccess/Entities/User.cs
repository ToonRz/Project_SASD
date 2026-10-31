namespace Webboard.DataAccess.Entities;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public string? TotpSecretProtected { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public long? TotpLastStep { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
