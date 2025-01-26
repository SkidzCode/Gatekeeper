namespace GateKeeper.Server.Extension;

public static class StringExtensions
{
    /// <summary>
    /// Removes control characters and trims the string to a maximum length for safer logging.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <param name="maxLength">Maximum allowed length. Default: 2000.</param>
    /// <returns>Sanitized string suitable for logging.</returns>
    public static string SanitizeForLogging(this string? input, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Remove control characters
        var sanitized = new string(input.Where(c => !char.IsControl(c)).ToArray());

        // Truncate if necessary
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength);
        }

        return sanitized;
    }
}