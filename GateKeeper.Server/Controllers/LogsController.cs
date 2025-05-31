using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GateKeeper.Server.Models.Configuration; // Ensured
using Microsoft.Extensions.Options; // Ensured

namespace GateKeeper.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILogger<LogsController> _logger;
        // private readonly IConfiguration _configuration; // Removed
        private readonly IOptions<SerilogConfig> _serilogConfigOptions; // Added
        private readonly bool _enableHashing = false;

        // For convenience, define a maximum we want to return
        private const int MaxLogEntries = 2000;

        public LogsController(
            ILogger<LogsController> logger,
            IOptions<SerilogConfig> serilogConfigOptions) // Modified parameters
        {
            _logger = logger;
            _serilogConfigOptions = serilogConfigOptions; // Assigned new field
            _enableHashing = _serilogConfigOptions.Value.EnableHashing; // Updated assignment
        }

        /// <summary>
        /// Reads the chained-log file for a specific date/time (UTC).
        /// If no dateTime is provided, it defaults to today's date at 00:00 UTC.
        /// 
        /// Assumes logs are chronological. We skip lines until we find the first log 
        /// whose timestamp >= targetDateTime, then collect up to 10k logs from there on.
        /// </summary>
        [HttpGet("chained-logs")]
        [AllowAnonymous] // or [Authorize(Roles = "Admin")]
        public IActionResult GetChainedLogs([FromQuery] DateTime? dateTime = null)
        {
            try
            {
                // Example: read from config or use a known path
                // var logsPath = _configuration["Logging:MainLogDirectory"];
                var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                if (!Directory.Exists(logsPath))
                {
                    return NotFound(new { error = $"Log directory not found: {logsPath}" });
                }

                // If no dateTime is provided, default to today's date at 00:00 UTC
                var targetDateTime = dateTime ?? DateTime.UtcNow.Date;

                // Build the file name for the day portion
                var fileName = $"chained-log-rotating-{targetDateTime:yyyy-MM-dd}.txt";
                var filePath = Path.Combine(logsPath, fileName);

                if (!_enableHashing)
                {
                    fileName = $"log-{targetDateTime:yyyyMMdd}.txt";
                    filePath = Path.Combine(logsPath, fileName);
                }
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new
                    {
                        error = $"No log file found for {targetDateTime:yyyy-MM-dd} at path: {filePath}"
                    });
                }

                // We'll read lines in a streaming fashion so we don't load the entire file.
                using var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    // IMPORTANT: allow read/write sharing
                    FileShare.ReadWrite
                );

                using var sr = new StreamReader(fs);

                var logEntries = new List<Dictionary<string, object>>(capacity: MaxLogEntries);

                string? line;
                int lineNumber = 0;
                int skippedLines = 0;
                bool foundStartTime = false; // we set this once we encounter the first >= targetDateTime

                while ((line = sr.ReadLine()) is not null)
                {
                    lineNumber++;

                    if (logEntries.Count >= MaxLogEntries)
                        break;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    Dictionary<string, JsonElement>? logObjectJson = null;
                    try
                    {
                        // Parse to Dictionary<string, JsonElement> 
                        // so we can inspect @t as a JsonElement.
                        logObjectJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(line);

                        // If it's null, the line was malformed
                        if (logObjectJson == null)
                        {
                            skippedLines++;
                            logEntries.Add(CreateFakeLogEntry(targetDateTime, lineNumber, "Skipped a malformed JSON line."));
                            continue;
                        }
                    }
                    catch (JsonException ex)
                    {
                        // Can't parse JSON at all
                        skippedLines++;
                        _logger.LogWarning(ex, "Invalid JSON line in file: {FilePath} at line {LineNumber}", filePath, lineNumber);
                        logEntries.Add(CreateFakeLogEntry(targetDateTime, lineNumber, "Skipped a malformed JSON line."));
                        continue;
                    }

                    // Now handle the actual time filtering
                    // If your logs are guaranteed to have a valid @t for every entry, just parse it
                    // Otherwise, handle the "no @t" or invalid @t" scenario.
                    if (logObjectJson.TryGetValue("@t", out var tElement))
                    {
                        if (tElement.ValueKind == JsonValueKind.String &&
                            DateTime.TryParse(tElement.GetString(), out var entryDateTime))
                        {
                            // If we haven't found the start time yet, skip lines until we do
                            if (!foundStartTime)
                            {
                                // If this line's time is still before the target, skip it
                                if (entryDateTime < targetDateTime)
                                    continue;

                                // We have found the first line that is >= target time
                                foundStartTime = true;
                            }

                            // We are now inside the time range, so add
                            logEntries.Add(ConvertJsonElementDict(logObjectJson));
                        }
                        else
                        {
                            // The original code added lines anyway if they had no parseable @t
                            // or if it was missing. So do that here, only if we already found the start time
                            // (OR if you want to match original logic exactly, just add them always.)
                            // For now, let's assume we match original logic = ALWAYS add.
                            logEntries.Add(ConvertJsonElementDict(logObjectJson));
                        }
                    }
                    else
                    {
                        // No @t key at all. The original code did not skip such lines, so we add them.
                        // But if logs truly are always guaranteed to have @t, this might never happen.
                        logEntries.Add(ConvertJsonElementDict(logObjectJson));
                    }
                }

                // Optionally, add a summary fake entry if any lines were skipped
                if (skippedLines > 0)
                {
                    logEntries.Add(CreateFakeLogEntry(
                        targetDateTime,
                        lineNumber + 1,
                        $"Total {skippedLines} malformed log entries were skipped."
                    ));
                }

                return Ok(logEntries);
            }
            catch (Exception ex)
            {
                // Removed generic catch block and HandleInternalError call, 
                // error will be handled by GlobalExceptionHandlerMiddleware
                throw; // Re-throw the exception to be caught by the global handler
            }
        }

        /// <summary>
        /// Creates a fake log entry indicating a skipped or malformed log line.
        /// </summary>
        private Dictionary<string, object> CreateFakeLogEntry(DateTime dateTime, int lineNumber, string message)
        {
            return new Dictionary<string, object>
            {
                { "@t", dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") },
                { "@mt", message },
                { "EnvironmentName", "Development" }, // Or fetch from configuration
                { "ProcessName", "GateKeeper.Server" },
                { "IsFake", true },
                { "SkippedLineNumber", lineNumber },
                { "OriginalContent", "[Content Skipped]" }
            };
        }

        /// <summary>
        /// Convert Dictionary<string, JsonElement> to Dictionary<string, object>
        /// so we can return it as part of the response.
        /// </summary>
        private Dictionary<string, object> ConvertJsonElementDict(Dictionary<string, JsonElement> source)
        {
            var result = new Dictionary<string, object>(source.Count);

            foreach (var kvp in source)
            {
                // If you want deep conversion (i.e. nested objects/arrays),
                // you might need a custom method to recursively handle kvp.Value.
                // For now, let's assume top-level properties are just strings/numbers/etc.
                // So we do `GetRawText()` or try `GetString()/GetInt32()` etc.
                // A simple approach: return them as raw JSON strings or best-effort parse.

                // If you know the property is always a string, DateTime, int, etc. 
                // you can parse more specifically.

                var jsonElement = kvp.Value;
                object? finalValue;

                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        finalValue = jsonElement.GetString();
                        break;
                    case JsonValueKind.Number:
                        if (jsonElement.TryGetInt64(out var longVal))
                        {
                            finalValue = longVal;
                        }
                        else if (jsonElement.TryGetDouble(out var doubleVal))
                        {
                            finalValue = doubleVal;
                        }
                        else
                        {
                            // Fallback to string
                            finalValue = jsonElement.GetRawText();
                        }
                        break;
                    case JsonValueKind.True:
                        finalValue = true;
                        break;
                    case JsonValueKind.False:
                        finalValue = false;
                        break;
                    case JsonValueKind.Undefined:
                    case JsonValueKind.Null:
                        finalValue = null;
                        break;
                    default:
                        // For Object or Array, you might want to do a deeper conversion.
                        // For simplicity, let's just store the raw JSON text.
                        finalValue = jsonElement.GetRawText();
                        break;
                }

                result[kvp.Key] = finalValue!;
            }

            return result;
        }

        // Removed HandleInternalError method as its functionality is now covered by GlobalExceptionHandlerMiddleware
    }
}
