
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GateKeeper.Server.Models.Configuration; // Added for PasswordSettingsConfig

namespace GateKeeper.Server.Services;

// ... (other using statements and class definition remain the same)

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

    public static async Task<bool> ValidatePasswordStrengthAsync(PasswordSettingsConfig passwordSettings, string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < passwordSettings.RequiredLength)
        {
            return false;
        }

        if (passwordSettings.RequireDigit && !Regex.IsMatch(password, @"\d"))
        {
            return false;
        }

        if (passwordSettings.RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
        {
            return false;
        }

        if (passwordSettings.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
        {
            return false;
        }

        // \W is for non-alphanumeric. This aligns with common interpretations.
        // If specific special characters were intended, the logic would need to be more complex.
        if (passwordSettings.RequireNonAlphanumeric && !Regex.IsMatch(password, @"\W"))
        {
            return false;
        }

        return await Task.FromResult(true); // Return true if all checks pass
    }
}