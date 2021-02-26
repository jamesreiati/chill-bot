using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.EventHandlers;
using Reiati.ChillBot.Services;
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
            using var bootstrapLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddDebug().AddEventSourceLogger());
            Program.Logger = bootstrapLoggerFactory.CreateLogger("Bootstrap");
            Program.Logger.LogInformation("Bootstrap logger initialized");

            Console.CancelKeyPress +=
                delegate (object sender, ConsoleCancelEventArgs args)
                {
                    Program.Logger.LogInformation("Shutdown initiated - console");
                };

            try
            {
                using IHost host = CreateHostBuilder(args).Build();

                var logger = host.Services.GetRequiredService<ILogger<Program>>();
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
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddJsonFile(HardCoded.Config.DefaultConfigFilePath, optional: true);
                    configBuilder.AddJsonFile(HardCoded.Config.LocalConfigFilePath, optional: true);
                    configBuilder.AddEnvironmentVariables(prefix: HardCoded.Config.EnvironmentVariablePrefix);
                    configBuilder.AddCommandLine(args);
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IMessageHandler, NewOptinGuildHandler>();
                    services.AddSingleton<IMessageHandler, JoinOptinGuildHandler>();
                    services.AddSingleton<IMessageHandler, ListOptinsGuildHandler>();
                    services.AddSingleton<IMessageHandler, HelpGuildHandler>();

                    services.AddSingleton<IMessageDispatcher, GeneralMessageHandler>();
                    services.AddSingleton<IUserJoinedHandler, UserJoinedHandler>();

                    services.AddHostedService<ChillBotService>();
                })
                .UseConsoleLifetime();
        }
    }
}
