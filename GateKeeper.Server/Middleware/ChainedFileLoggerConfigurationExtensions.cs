using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace GateKeeper.Server.Middleware
{
    public static class ChainedFileLoggerConfigurationExtensions
    {
        /// <summary>
        /// Extension method to add the ChainedFileSink to the Serilog pipeline.
        /// </summary>
        public static LoggerConfiguration ChainedFile(
            this LoggerSinkConfiguration sinkConfiguration,
            string mainLogFilePath,
            string hashesOnlyFilePath,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            return sinkConfiguration.Sink(
                new ChainedFileSink(mainLogFilePath, hashesOnlyFilePath, new CompactJsonFormatter()),
                restrictedToMinimumLevel
            );
        }
    }
}