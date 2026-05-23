using System.Security.Cryptography;

namespace MBappe.Services;

public class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(SaltSize);
    }

    public byte[] HashPassword(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: HashSize);
    }

    public bool VerifyPassword(string password, byte[] salt, byte[] expectedHash)
    {
        var actualHash = HashPassword(password, salt);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}