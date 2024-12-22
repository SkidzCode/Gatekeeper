
using System;
using System.Security.Cryptography;
using System.Text;
namespace GateKeeper.Server.Services;

public static class PasswordHelper
{
    // Generate a random salt
    public static string GenerateSalt()
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] saltBytes = new byte[16]; // 16-byte salt (128 bits)
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    // Hash a password using PBKDF2 with a given salt
    public static string HashPassword(string password, string salt)
    {
        // Convert salt and password to byte arrays
        var saltBytes = Convert.FromBase64String(salt);
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        // Combine password and salt
        using var rfc2898 = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 100_000, HashAlgorithmName.SHA512);
        // Generate a 256-bit (32-byte) hash
        var hashBytes = rfc2898.GetBytes(32);
        return Convert.ToBase64String(hashBytes);
    }
}