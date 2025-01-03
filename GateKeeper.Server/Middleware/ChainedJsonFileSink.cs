using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GateKeeper.Server.Middleware
{
    public class ChainedFileSink : ILogEventSink
    {
        private readonly string _mainLogFilePath;
        private readonly string _hashesOnlyFilePath;
        private readonly ITextFormatter _formatter;

        private string _previousHash = string.Empty;
        private readonly object _syncRoot = new();

        /// <summary>
        /// Constructor for the custom chained file sink.
        /// </summary>
        /// <param name="mainLogFilePath">The path to the main JSON log file.</param>
        /// <param name="hashesOnlyFilePath">The path to the file that stores only the chain-hashes.</param>
        /// <param name="formatter">The Serilog formatter to render the log event (e.g., CompactJsonFormatter).</param>
        public ChainedFileSink(string mainLogFilePath, string hashesOnlyFilePath, ITextFormatter? formatter = null)
        {
            _mainLogFilePath = mainLogFilePath;
            _hashesOnlyFilePath = hashesOnlyFilePath;
            _formatter = formatter ?? new CompactJsonFormatter(); // Use Compact JSON by default

            // On startup, try loading the last line from the main file to retrieve the previous chain hash.
            if (File.Exists(_mainLogFilePath))
            {
                var lastLine = File.ReadLines(_mainLogFilePath).LastOrDefault();
                if (!string.IsNullOrEmpty(lastLine))
                {
                    // Attempt to parse the last line as JSON
                    // and extract the "ChainHash" property if it exists.
                    try
                    {
                        using var doc = JsonDocument.Parse(lastLine);
                        if (doc.RootElement.TryGetProperty("ChainHash", out var chainHashElement))
                        {
                            _previousHash = chainHashElement.GetString() ?? string.Empty;
                        }
                    }
                    catch
                    {
                        // If for some reason the file is corrupted or not JSON, just ignore.
                    }
                }
            }
        }

        /// <summary>
        /// This method is called by Serilog for each log event.
        /// </summary>
        /// <param name="logEvent"></param>
        public void Emit(LogEvent logEvent)
        {
            lock (_syncRoot)
            {
                // 1) Render the log event as JSON using the provided formatter.
                var rawJson = FormatLogEventAsString(logEvent);

                // 2) Compute the new chain hash = SHA256( previousHash + rawJson ).
                var hashInput = _previousHash + rawJson;
                var currentHash = ComputeSha256Base64(hashInput);

                // 3) Augment the JSON with a new property "ChainHash" = currentHash.
                //    We do this by parsing the JSON, then re-writing it with the extra property.
                var augmentedJson = AugmentJsonWithChainHash(rawJson, currentHash);

                // 4) Append the augmented JSON to the main log file.
                File.AppendAllText(_mainLogFilePath, augmentedJson + Environment.NewLine);

                // 5) Append the current hash to the "hashes-only" file.
                File.AppendAllText(_hashesOnlyFilePath, currentHash + Environment.NewLine);

                // 6) Update the _previousHash to the current one for the next iteration.
                _previousHash = currentHash;
            }
        }

        /// <summary>
        /// Use the configured formatter to write the log event into a string.
        /// </summary>
        private string FormatLogEventAsString(LogEvent logEvent)
        {
            using var sw = new StringWriter();
            _formatter.Format(logEvent, sw);
            return sw.ToString().TrimEnd('\r', '\n');
        }

        /// <summary>
        /// Compute a SHA256 hash of the input string, and return it as a Base64-encoded string.
        /// </summary>
        private static string ComputeSha256Base64(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Parse the existing log JSON, add "ChainHash" property, and re-serialize.
        /// </summary>
        private static string AugmentJsonWithChainHash(string originalJson, string chainHash)
        {
            try
            {
                using var doc = JsonDocument.Parse(originalJson);
                // We'll build a new JSON object that merges the existing properties + the new ChainHash property.
                var root = doc.RootElement;

                // Use System.Text.Json's JsonElement -> JsonObject (via a dictionary approach).
                var dict = new Dictionary<string, object>();

                foreach (var prop in root.EnumerateObject())
                {
                    // Copy existing properties (propertyName -> propertyValue)
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString()!,
                        JsonValueKind.Number => prop.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Object => prop.Value.GetRawText(), // Nested JSON
                        JsonValueKind.Array => prop.Value.GetRawText(), // Arrays
                        JsonValueKind.Null => null,
                        _ => prop.Value.GetRawText(),
                    };
                }

                // Add the new property
                dict["ChainHash"] = chainHash;

                // Re-serialize
                return JsonSerializer.Serialize(dict);
            }
            catch
            {
                // If parsing fails for some reason, fallback to just adding a suffix
                // Not ideal, but we won't fail the logging pipeline.
                return originalJson.TrimEnd('}', ' ') + $", \"ChainHash\": \"{chainHash}\"}}";
            }
        }
    }
}
