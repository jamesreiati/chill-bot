using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Reiati.ChillBot.Tools
{
    /// <summary>
    /// Static manager for creating <see cref="ILogger"/> instances.
    /// </summary>
    public sealed class LogManager
    {
        /// <summary>
        /// The factory to use to create <see cref="ILogger"/> instances.
        /// </summary>
        private static ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

        /// <summary>
        /// Configure the <see cref="LogManager"/> to use the provided <see cref="ILoggerFactory"/> when creating logger instances.
        /// </summary>
        /// <param name="loggerFactory">The factory used to create new logger instances.</param>
        public static void Configure(ILoggerFactory loggerFactory)
        {
            ValidateArg.IsNotNull(loggerFactory, nameof(loggerFactory));
            LogManager.loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance using the provided category name.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>A new <see cref="ILogger"/> instance.</returns>
        public static ILogger GetLogger(string categoryName)
        {
            return loggerFactory?.CreateLogger(categoryName);
        }

        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance using the full name of the given type.
        /// </summary>
        /// <param name="type">The type to use as the category name for messages produced by the logger.</param>
        /// <returns>A new <see cref="ILogger"/> instance.</returns>
        public static ILogger GetLogger(Type type)
        {
            return loggerFactory?.CreateLogger(type);
        }
    }
}
