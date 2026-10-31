namespace Webboard.Api.Security;

public class JwtOptions
{
    public const int MinKeyBytes = 32;

    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SigningKey { get; set; } = "";

    public bool HasValidKey()
    {
        Span<byte> buffer = new byte[SigningKey.Length];
        return Convert.TryFromBase64String(SigningKey, buffer, out var written) && written >= MinKeyBytes;
    }

    public byte[] GetKeyBytes()
    {
        if (!HasValidKey())
        {
            throw new InvalidOperationException(
                $"'Jwt:SigningKey' must be a base64 string of at least {MinKeyBytes} bytes. " +
                "Set it with 'dotnet user-secrets set Jwt:SigningKey <value> --project src/Webboard.Api' or the environment variable Jwt__SigningKey.");
        }

        return Convert.FromBase64String(SigningKey);
    }
}
