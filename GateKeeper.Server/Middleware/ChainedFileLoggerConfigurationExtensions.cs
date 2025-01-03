using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace GateKeeper.Server.Middleware
{
    public static class ChainedFileLoggerConfigurationExtensions
    {
        /// <summary>
        /// Extension method to add the ChainedFileSink (with daily rolling) to the Serilog pipeline.
        /// </summary>
        public static LoggerConfiguration ChainedFile(
            this LoggerSinkConfiguration sinkConfiguration,
            string mainLogDirectory,
            string hashesOnlyDirectory,
            string fileNamePrefix = "chained-log",
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            var sink = new ChainedFileSink(mainLogDirectory, hashesOnlyDirectory, fileNamePrefix, new CompactJsonFormatter());
            return sinkConfiguration.Sink(sink, restrictedToMinimumLevel);
        }
    }
}