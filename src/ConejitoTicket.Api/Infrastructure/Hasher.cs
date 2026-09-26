using System.Security.Cryptography;

namespace ConejitoTicket.Api.Infrastructure;

// Formato: "{iteraciones}.{salt base64}.{hash base64}" (PBKDF2-SHA256).
public static class Hasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(value, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string value, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(value, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
