using System.Security.Cryptography;
using System.Text;
namespace CompVault.Backend.Features.Helpers;

/// <summary>
/// Hjelpeklasse for å hashe en OTP. Brukes både i backend og tester. Kan endre algoritmen, uten å endre
/// alle steder
/// </summary>
public static class OtpHasher
{
    /// <summary>
    /// Hasher en kode for å slippe å lagre koden i klartekst med tanke på databasebrudd
    /// </summary>
    /// <param name="code">String med en kode</param>
    /// <returns>En hashet string med 64-tegn</returns>
    public static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}