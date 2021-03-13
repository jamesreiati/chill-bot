using Discord;
using Microsoft.Extensions.Logging;

namespace Reiati.ChillBot.Tools
{
    /// <summary>
    /// Extension methods related to logging.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Converts a Discord <see cref="LogSeverity"/> to a corresponding <see cref="LogLevel"/>.
        /// </summary>
        /// <param name="logSeverity">The Discord log severity.</param>
        /// <returns>The <see cref="LogLevel"/> corresponding to the <see cref="LogSeverity"/>.</returns>
        public static LogLevel ToLogLevel(this LogSeverity logSeverity)
        {
            switch (logSeverity)
            {
                case LogSeverity.Debug:
                    return LogLevel.Trace;
                case LogSeverity.Verbose:
                    return LogLevel.Debug;
                case LogSeverity.Info:
                    return LogLevel.Information;
                case LogSeverity.Warning:
                    return LogLevel.Warning;
                case LogSeverity.Error:
                    return LogLevel.Error;
                case LogSeverity.Critical:
                    return LogLevel.Critical;
                default:
                    return LogLevel.None;
            }
        }

        /// <summary>
        /// Converts a <see cref="LogLevel"/> to a corresponding Discord <see cref="LogSeverity"/>.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <returns>The <see cref="LogSeverity"/> corresponding to the <see cref="LogLevel"/>.</returns>
        public static LogSeverity ToLogSeverity(this LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return LogSeverity.Debug;
                case LogLevel.Debug:
                    return LogSeverity.Verbose;
                case LogLevel.Information:
                    return LogSeverity.Info;
                case LogLevel.Warning:
                    return LogSeverity.Warning;
                case LogLevel.Error:
                    return LogSeverity.Error;
                case LogLevel.Critical:
                default:
                    return LogSeverity.Critical;
            }
        }
    }
}
