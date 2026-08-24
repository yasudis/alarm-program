using AlarmProgram.Infrastructure.Security;

namespace AlarmProgram.Tests.Unit.Security;

public class DpapiSecretProtectorTests
{
    [Fact]
    public void Protect_and_Unprotect_roundtrip_secret()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var protector = new DpapiSecretProtector();
        const string secret = "123456789:AAExampleTelegramBotTokenValue123456";

        var protectedText = protector.Protect(secret);

        Assert.NotEqual(secret, protectedText);
        Assert.DoesNotContain(secret, protectedText);
        Assert.StartsWith("enc.v1:", protectedText);
        Assert.Equal(secret, protector.Unprotect(protectedText));
    }

    [Fact]
    public void Unprotect_returns_plaintext_without_prefix_unchanged()
    {
        var protector = new DpapiSecretProtector();

        Assert.Equal("plain-value", protector.Unprotect("plain-value"));
        Assert.Equal(string.Empty, protector.Protect(string.Empty));
    }
}
