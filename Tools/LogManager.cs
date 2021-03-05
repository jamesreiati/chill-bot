using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;

namespace Reiati.ChillBot.Tools
{
    /// <summary>
    /// Static manager for obtaining <see cref="ILogger"/> instances.
    /// </summary>
    public sealed class LogManager
    {
        /// <summary>
        /// The factory to use to create <see cref="ILogger"/> instances.
        /// </summary>
        private static ILoggerFactory LoggerFactory = NullLoggerFactory.Instance;

        /// <summary>
        /// A cache of loggers that were created.
        /// </summary>
        private static ConcurrentDictionary<string, ILogger> LoggerCache = new ConcurrentDictionary<string, ILogger>();

        /// <summary>
        /// Configure the <see cref="LogManager"/> to use the provided <see cref="ILoggerFactory"/> when creating logger instances.
        /// </summary>
        /// <param name="loggerFactory">The factory used to create new logger instances.</param>
        public static void Configure(ILoggerFactory loggerFactory)
        {
            ValidateArg.IsNotNull(loggerFactory, nameof(loggerFactory));
            LogManager.LoggerFactory = loggerFactory;
        }

        /// <summary>
        /// Gets or creates a <see cref="ILogger"/> instance using the provided category name.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>A new <see cref="ILogger"/> instance.</returns>
        public static ILogger GetLogger(string categoryName)
        {
            return LogManager.LoggerCache.GetOrAdd(categoryName, (_) => LoggerFactory.CreateLogger(categoryName));
        }

        /// <summary>
        /// Gets or creates a <see cref="ILogger"/> instance using the full name of the given type.
        /// </summary>
        /// <param name="type">The type to use as the category name for messages produced by the logger.</param>
        /// <returns>A new <see cref="ILogger"/> instance.</returns>
        public static ILogger GetLogger(Type type)
        {
            return LogManager.LoggerCache.GetOrAdd(type.FullName, (_) => LoggerFactory.CreateLogger(type));
        }
    }
}
