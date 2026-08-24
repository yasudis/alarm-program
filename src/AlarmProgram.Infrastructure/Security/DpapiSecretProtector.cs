using System.Security.Cryptography;
using System.Text;
using AlarmProgram.Application.Abstractions;

namespace AlarmProgram.Infrastructure.Security;

public sealed class DpapiSecretProtector : ISecretProtector
{
    private const string Prefix = "enc.v1:";

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string protectedText)
    {
        if (string.IsNullOrEmpty(protectedText) || !protectedText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return protectedText;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(protectedText[Prefix.Length..]);
            var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            throw new InvalidOperationException("Не удалось расшифровать секрет. Файл настроек поврежден или создан другим пользователем.", ex);
        }
    }
}
