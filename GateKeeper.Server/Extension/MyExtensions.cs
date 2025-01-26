using System.Runtime.CompilerServices;
using System.Web;

namespace GateKeeper.Server.Extension;

public static class MyExtensions
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

    public static void LogMyError(
        this ILogger logger,
        Exception ex,
        string functionName = "",
        params (string Key, object Value)[] data
    )
    {
        // Build up a string like: "FromId='123', ToName='Bob', ToEmail='bob@test.com'"
        var paramString = string.Join(", ", data.Select(d => $"'{{{d.Key}}}'"));
        
        object[] objectArray = new object[] { functionName }
            .Concat(data.Select(item => (object)item)) // Add the tuple objects
            .Concat(new object[] { ex.Message })        // Add the end string
            .ToArray();

        // Log in a consistent format:
        // {FunctionName}(FromId='123', ToName='Bob', ToEmail='bob@test.com') Error: Something went wrong
        logger.LogError(
            ex,
            "{FunctionName}(" + paramString + ") - Error: {ErrorMessage}",
            objectArray
        );
    }
}
