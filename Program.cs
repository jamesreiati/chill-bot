using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Services;
using Reiati.ChillBot.Tools;
using System;

namespace Reiati.ChillBot
{
    /// <summary>
    /// Class containing the main entry point for the application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILogger Logger;

        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            // Construct a logger separate from the host that can be used when we can't use host's logger 
            // due to the host being disposed or not yet created.
            using var bootstrapLoggerFactory = LoggerFactory.Create(Program.ConfigureBaseLogging);
            Program.Logger = bootstrapLoggerFactory.CreateLogger("Bootstrap");
            Program.Logger.LogInformation("Bootstrap logger initialized");

            Console.CancelKeyPress += 
                delegate(object sender, ConsoleCancelEventArgs args)
                {
                    Program.Logger.LogInformation("Shutdown initiated - console");
                };

            try
            {
                using IHost host = CreateHostBuilder(args).Build();

                // Configure our LogManager with the host's LoggerFactory
                LogManager.Configure(host.Services.GetRequiredService<ILoggerFactory>());
                var logger = LogManager.GetLogger(typeof(Program));
                logger.LogInformation("Host logger initialized");

                host.Run();
            }
            catch (Exception e)
            {
                Program.Logger.LogError(e, "Shutdown initiated - exception thrown");
            }
        }

        /// <summary>
        /// Creates and configures the builder for the host application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns></returns>
        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging((host, logging) =>
                {
                    Program.ConfigureBaseLogging(logging);

                    // Configure Application Insights if a connection string or instrumentation key is provided
                    string connectionString = host.Configuration[HardCoded.Config.ApplicationInsightsConnectionStringConfigKey];
                    string instrumentationKey = host.Configuration[HardCoded.Config.ApplicationInsightsInstrumentationKeyConfigKey];
                    if (!string.IsNullOrEmpty(connectionString) || !string.IsNullOrEmpty(instrumentationKey))
                    {
                        logging.AddApplicationInsights(config =>
                        {
                            // Prefer using the connection string if provided since the instrumentation key is obsolete.
                            // See https://github.com/microsoft/ApplicationInsights-dotnet/issues/2560 for more details.
                            if (!string.IsNullOrEmpty(connectionString))
                            {
                                config.ConnectionString = connectionString;
                            }
                            else if (!string.IsNullOrEmpty(instrumentationKey))
                            {
                                config.InstrumentationKey = instrumentationKey;
                            }
                        }, options =>
                        {
                        });
                    }
                })
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddJsonFile(HardCoded.Config.DefaultConfigFilePath, optional: true);
                    configBuilder.AddJsonFile(HardCoded.Config.LocalConfigFilePath, optional: true);
                    configBuilder.AddEnvironmentVariables(prefix: HardCoded.Config.EnvironmentVariablePrefix);
                    configBuilder.AddCommandLine(args);
                })
                .ConfigureServices((host, services) =>
                {
                    services.AddMemoryCache();

                    // Get the type of guild repository to use, defaulting to GuildRepositoryType.File
                    GuildRepositoryType guildRepositoryType = host.Configuration.GetValue(HardCoded.Config.GuildRepositoryTypeConfigKey, GuildRepositoryType.File);

                    // Add a singleton for the guild repository
                    switch (guildRepositoryType)
                    {
                        case GuildRepositoryType.File:
                        default:
                            services.AddSingleton<IGuildRepository>(FileBasedGuildRepository.Instance);
                            break;
                        case GuildRepositoryType.AzureBlob:
                            string connectionString = host.Configuration[string.Format(HardCoded.Config.GuildRepositoryConnectionStringConfigKeyFormat, guildRepositoryType)];
                            string container = host.Configuration[string.Format(HardCoded.Config.GuildRepositoryContainerConfigKeyFormat, guildRepositoryType)];
                            services.AddSingleton<IGuildRepository>(new AzureBlobGuildRepository(connectionString, container));
                            break;
                    }

                    // Add services and configuration for the Discord client
                    var socketConfig = new DiscordSocketConfig
                    {
                        TotalShards = 1,
                        LogLevel = Program.GetMinimumDiscordLogLevel(host.Configuration).ToLogSeverity(),
                        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers
                    };

                    services.AddSingleton(socketConfig);
                    services.AddSingleton<DiscordShardedClient>();

                    // Add services and configuration for the Discord interaction service that handles application commands.
                    var interactionServiceConfig = new InteractionServiceConfig
                    {
                        LogLevel = Program.GetMinimumDiscordLogLevel(host.Configuration).ToLogSeverity(),
                    };

                    services.AddSingleton(interactionServiceConfig);
                    services.AddSingleton<InteractionService>();

                    // Add services for caching opt-in channels
                    services.AddSingleton<IOptinChannelCache, OptinChannelMemoryCache>();
                    services.AddSingleton<IOptinChannelCacheManager, OptinChannelCacheManager>();

                    // Add the main service for Chill Bot
                    services.AddHostedService<ChillBotService>();
                })
                .UseConsoleLifetime();
        }

        /// <summary>
        /// Get the minimum Discord category log level configured for any <see cref="Microsoft.Extensions.Logging"/> provider.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>The minimum log level configured for Discord logs.</returns>
        private static LogLevel GetMinimumDiscordLogLevel(IConfiguration configuration)
        {
            bool logLevelConfigured = false;
            LogLevel minimumDiscordLogLevel = LogLevel.None;
            foreach (var configSetting in configuration.GetSection(nameof(Microsoft.Extensions.Logging)).AsEnumerable())
            {
                // Look for config settings assigning a LogLevel to a Discord category name (including subcategories of Discord)
                // or to the default logging category.
                if (configSetting.Key.Contains($":{nameof(LogLevel)}:{nameof(Discord)}", StringComparison.OrdinalIgnoreCase) ||
                    configSetting.Key.EndsWith($":{nameof(LogLevel)}:Default", StringComparison.OrdinalIgnoreCase))
                {
                    // Check for a new minimum log level
                    if (Enum.TryParse(configSetting.Value, out LogLevel logLevel) && logLevel < minimumDiscordLogLevel)
                    {
                        minimumDiscordLogLevel = logLevel;
                        logLevelConfigured = true;
                    }
                }
            }

            // Return the minimum configured log level or default to LogLevel.Information if not configured.
            return logLevelConfigured ? minimumDiscordLogLevel : LogLevel.Information;
        }

        /// <summary>
        /// Configures an <see cref="ILoggingBuilder"/> with the base logging providers.
        /// </summary>
        /// <param name="loggingBuilder">The logging builder to configure.</param>
        private static void ConfigureBaseLogging(ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConsole();
            loggingBuilder.AddDebug();
            loggingBuilder.AddEventSourceLogger();
        }
    }
}
