using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using OtpNet;

namespace Webboard.Api.Security;

public class TotpService(IDataProtectionProvider dataProtection, TimeProvider time)
{
    private const string Purpose = "Webboard.TotpSecret";
    private const int KeyBytes = 20;

    private readonly IDataProtector _protector = dataProtection.CreateProtector(Purpose);

    public static string NewSharedKey() => Base32Encoding.ToString(RandomNumberGenerator.GetBytes(KeyBytes));

    public static string BuildOtpAuthUri(string username, string sharedKey) =>
        $"otpauth://totp/Webboard:{username}?secret={sharedKey}&issuer=Webboard&algorithm=SHA1&digits=6&period=30";

    public string Protect(string sharedKey) => _protector.Protect(sharedKey);

    public string Unprotect(string protectedKey) => _protector.Unprotect(protectedKey);

    /// <summary>Returns the matched 30-second time step (±1 step tolerated), or null when the code is wrong.</summary>
    public long? Match(string sharedKey, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(sharedKey));
        return totp.VerifyTotp(time.GetUtcNow().UtcDateTime, code, out var step, new VerificationWindow(previous: 1, future: 1))
            ? step
            : null;
    }
}
