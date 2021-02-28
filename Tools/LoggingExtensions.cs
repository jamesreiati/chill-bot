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
    }
}
