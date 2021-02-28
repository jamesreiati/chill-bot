using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

                    // Configure Application Insights if an instrumentation key is provided
                    string instrumentationKey = host.Configuration[HardCoded.Config.ApplicationInsightsInstrumentationKeyConfigKey];
                    if (!string.IsNullOrEmpty(instrumentationKey))
                    {
                        logging.AddApplicationInsights(instrumentationKey);
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
                    // Get the type of guild repository to use, defaulting to GuildRepositoryType.File
                    if (!Enum.TryParse(host.Configuration[HardCoded.Config.GuildRepositoryTypeConfigKey], out GuildRepositoryType guildRepositoryType))
                    {
                        guildRepositoryType = GuildRepositoryType.File;
                    }

                    // Add a singleton for the guild repository
                    switch (guildRepositoryType)
                    {
                        case GuildRepositoryType.File:
                        default:
                            services.AddSingleton<IGuildRepository>(FileBasedGuildRepository.Instance);
                            break;
                    }

                    services.AddHostedService<ChillBotService>();
                })
                .UseConsoleLifetime();
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
