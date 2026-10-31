using Webboard.Api.Security;

namespace Webboard.Api.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentStringsThatBothVerify()
    {
        var first = PasswordHasher.Hash("webboard123");
        var second = PasswordHasher.Hash("webboard123");

        Assert.NotEqual(first, second);
        Assert.StartsWith("PBKDF2-SHA256$600000$", first);
        Assert.True(PasswordHasher.Verify("webboard123", first));
        Assert.True(PasswordHasher.Verify("webboard123", second));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("webboard123");

        Assert.False(PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void Verify_DummyHash_AlwaysFalse()
    {
        Assert.False(PasswordHasher.Verify("webboard123", PasswordHasher.DummyHash));
        Assert.False(PasswordHasher.Verify("", PasswordHasher.DummyHash));
    }

    [Fact]
    public void Verify_KnownAnswerFromPythonHashlib_Passes()
    {
        // salt = bytes(1..16); hashlib.pbkdf2_hmac('sha256', b'webboard123', salt, 600000, 32)
        const string stored =
            "PBKDF2-SHA256$600000$AQIDBAUGBwgJCgsMDQ4PEA==$xABGuHstpQb4K1xhvOxvKOhBp+EDDPBOsPpDtrlkPe4=";

        Assert.True(PasswordHasher.Verify("webboard123", stored));
        Assert.False(PasswordHasher.Verify("webboard124", stored));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plaintext")]
    [InlineData("PBKDF2-SHA256$abc$AQID$AQID")]
    [InlineData("PBKDF2-SHA256$600000$not-base64$AQID")]
    [InlineData("OTHER$600000$AQID$AQID")]
    public void Verify_MalformedStoredValue_ReturnsFalse(string stored)
    {
        Assert.False(PasswordHasher.Verify("webboard123", stored));
    }
}
