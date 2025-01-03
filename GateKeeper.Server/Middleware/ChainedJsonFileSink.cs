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
        private readonly string _mainLogDirectory;
        private readonly string _hashesOnlyDirectory;
        private readonly string _fileNamePrefix;
        private readonly ITextFormatter _formatter;

        // We will build new filenames daily (or at any interval you choose).
        private DateTime _currentDate;
        private string _currentMainLogFilePath = string.Empty;
        private string _currentHashesFilePath = string.Empty;

        // This holds the last computed hash in the chain, which we carry over
        // even when rotating to a new file (if you want the chain to continue).
        private string _previousHash = string.Empty;

        private readonly object _syncRoot = new();

        /// <summary>
        /// Constructor for the custom chained file sink with rotating filenames.
        /// </summary>
        /// <param name="mainLogDirectory">Directory where JSON logs go.</param>
        /// <param name="hashesOnlyDirectory">Directory where hash-only logs go.</param>
        /// <param name="fileNamePrefix">Prefix for the daily filenames, e.g. "chained-log".</param>
        /// <param name="formatter">Optional Serilog formatter to render the log event (defaults to CompactJsonFormatter).</param>
        public ChainedFileSink(
            string mainLogDirectory,
            string hashesOnlyDirectory,
            string fileNamePrefix = "chained-log",
            ITextFormatter? formatter = null)
        {
            _mainLogDirectory = mainLogDirectory;
            _hashesOnlyDirectory = hashesOnlyDirectory;
            _fileNamePrefix = fileNamePrefix;

            // Use Compact JSON by default.
            _formatter = formatter ?? new CompactJsonFormatter();

            // Ensure directories exist.
            Directory.CreateDirectory(_mainLogDirectory);
            Directory.CreateDirectory(_hashesOnlyDirectory);

            // On startup, initialize today's file paths
            _currentDate = DateTime.UtcNow.Date;
            RollFileIfNeeded(force: true);
        }

        /// <summary>
        /// This method is called by Serilog for each log event.
        /// </summary>
        /// <param name="logEvent"></param>
        public void Emit(LogEvent logEvent)
        {
            lock (_syncRoot)
            {
                // 1) Check if a rotation is needed (if date changed).
                RollFileIfNeeded();

                // 2) Render the log event as JSON using the formatter.
                var rawJson = FormatLogEventAsString(logEvent);

                // 3) Compute the new chain hash = SHA256(previousHash + rawJson).
                var hashInput = _previousHash + rawJson;
                var currentHash = ComputeSha256Base64(hashInput);

                // 4) Augment the JSON with a new property "ChainHash" = currentHash.
                var augmentedJson = AugmentJsonWithChainHash(rawJson, currentHash);

                // 5) Append the augmented JSON to the main log file.
                File.AppendAllText(_currentMainLogFilePath, augmentedJson + Environment.NewLine);

                // 6) Append the current hash to the "hashes-only" file.
                File.AppendAllText(_currentHashesFilePath, currentHash + Environment.NewLine);

                // 7) Update the _previousHash to the current one for the next iteration.
                _previousHash = currentHash;
            }
        }

        /// <summary>
        /// Decides if we need to roll (rotate) to a new file, based on the date.
        /// </summary>
        /// <param name="force">If true, always creates the file regardless of date check.</param>
        private void RollFileIfNeeded(bool force = false)
        {
            var nowDate = DateTime.UtcNow.Date;
            if (force || nowDate > _currentDate)
            {
                // Move to a new date
                _currentDate = nowDate;

                // Build filenames like: "Logs\chained-log-2025-01-02.txt"
                var dateString = _currentDate.ToString("yyyy-MM-dd");
                _currentMainLogFilePath = Path.Combine(
                    _mainLogDirectory,
                    $"{_fileNamePrefix}-{dateString}.txt"
                );
                _currentHashesFilePath = Path.Combine(
                    _hashesOnlyDirectory,
                    $"{_fileNamePrefix}-hashes-{dateString}.txt"
                );

                // Optional: if you want each day to have its own chain, reset:
                // _previousHash = string.Empty;
                //
                // If you want to *continue* the chain from yesterday’s final hash,
                // we must read the last chain hash from the new file (if it exists).
                // That means if the new file already exists for today (like if the app restarted),
                // we attempt to load the last chain hash from it.
                // 1) If today's file exists, read from it:
                if (File.Exists(_currentMainLogFilePath))
                {
                    LoadLastChainHash(_currentMainLogFilePath);
                }
                else
                {
                    // 2) If today's file does NOT exist, look for the NEWEST file in the directory.
                    //    We'll assume your log files match the pattern "chained-log-YYYY-MM-DD.txt".
                    var pattern = $"{_fileNamePrefix}-*.txt";
                    var allFiles = Directory.GetFiles(_mainLogDirectory, pattern);
                    if (allFiles.Length <= 0) return;
                    
                    // Option A) Sort by creation time (descending):
                    // var newestFile = allFiles
                    //     .OrderByDescending(f => new FileInfo(f).CreationTimeUtc)
                    //     .First();

                    // Option B) Sort by filename (descending),
                    // assuming the date format is in ascending lexical order (yyyy-MM-dd):
                    var newestFile = allFiles
                        .OrderByDescending(f => f)  // if names are "chained-log-2025-01-02.txt"
                        .First();

                    LoadLastChainHash(newestFile);
                }

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
                var root = doc.RootElement;

                // We'll build a new JSON object that merges existing properties + new ChainHash property.
                var dict = new Dictionary<string, object>();

                foreach (var prop in root.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString()!,
                        JsonValueKind.Number => prop.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Object => prop.Value.GetRawText(), // Nested JSON
                        JsonValueKind.Array => prop.Value.GetRawText(),  // Arrays
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
                // If parsing fails, fallback to just appending.
                return originalJson.TrimEnd('}', ' ')
                    + $", \"ChainHash\": \"{chainHash}\"}}";
            }
        }

        /// <summary>
        /// Attempts to read the last log line from the file, parse out the "ChainHash",
        /// and store it in _previousHash.
        /// </summary>
        private void LoadLastChainHash(string filePath)
        {
            var lastLine = File.ReadLines(filePath).LastOrDefault();
            if (string.IsNullOrEmpty(lastLine)) return;
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
                // If for some reason it's not valid JSON, ignore
            }
        }

    }
}
