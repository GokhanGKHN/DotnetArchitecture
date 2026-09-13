using System.Security.Cryptography;
using DotnetArchitecture.Application.Interfaces;

namespace DotnetArchitecture.Persistence.Services;

/// <summary>
/// PBKDF2 (SHA-256) ve kriptografik tuzlama (salt) kullanan güvenli şifre hash'leme servisi.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        var inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        // Zamanlama saldırılarını (timing attack) önlemek için sabit zamanlı karşılaştırma
        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}
