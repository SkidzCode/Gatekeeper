
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

    public static async Task<bool> ValidatePasswordStrengthAsync(IConfiguration _configuration, string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        // Get password strength criteria from appsettings.json
        var minLength = Convert.ToInt32(_configuration["PasswordStrength:MinLength"]);
        var requireUppercase = Convert.ToBoolean(_configuration["PasswordStrength:RequireUppercase"]);
        var requireLowercase = Convert.ToBoolean(_configuration["PasswordStrength:RequireLowercase"]);
        var requireDigit = Convert.ToBoolean(_configuration["PasswordStrength:RequireDigit"]);
        var requireSpecialChar = Convert.ToBoolean(_configuration["PasswordStrength:RequireSpecialChar"]);
        var specialChars = _configuration["PasswordStrength:SpecialChars"];

        // Check length
        if (password.Length < minLength)
        {
            return false;
        }

        // Check for uppercase
        if (requireUppercase && !Regex.IsMatch(password, "[A-Z]"))
        {
            return false;
        }

        // Check for lowercase
        if (requireLowercase && !Regex.IsMatch(password, "[a-z]"))
        {
            return false;
        }

        // Check for digit
        if (requireDigit && !Regex.IsMatch(password, "[0-9]"))
        {
            return false;
        }

        // Simplified Special Character Check:
        if (requireSpecialChar && !password.Any(specialChars.Contains))
        {
            return false;
        }

        return await Task.FromResult(true);
    }
}