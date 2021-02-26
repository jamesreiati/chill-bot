using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Services;
using System;
using System.IO;

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
        private static ILog Logger;

        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            if (!Program.TryStartLogger())
            {
                Console.WriteLine(string.Format(
                    "Application Exit - No logging config found;{{filePath:{0}}}",
                    HardCoded.Logging.ConfigFilePath));
                return;
            }

            Console.CancelKeyPress += 
                delegate(object sender, ConsoleCancelEventArgs args)
                {
                    Program.Logger.Info("Shutdown initiated - console");
                };

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Program.Logger.ErrorFormat("Shutdown initiated - exception thrown;{{exception:{0}}}", e.ToString());
            }
        }

        /// <summary>
        /// Tries to start the logger.
        /// </summary>
        private static bool TryStartLogger()
        {
            // Required for colored console output: https://issues.apache.org/jira/browse/LOG4NET-658
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var configFile = new FileInfo(HardCoded.Logging.ConfigFilePath);

            if (!configFile.Exists)
            {
                return false;
            }

            XmlConfigurator.Configure(configFile);
            Program.Logger = LogManager.GetLogger("Bootstrap");
            Program.Logger.InfoFormat("Logger initialized;{{loggingConfig:{0}}}", configFile.FullName);
            return true;
        }

        /// <summary>
        /// Creates and configures the builder for the host application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns></returns>
        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => logging.ClearProviders())  // Logging is configured outside of the Host Builder
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddJsonFile(HardCoded.Config.DefaultConfigFilePath, optional: true);
                    configBuilder.AddJsonFile(HardCoded.Config.LocalConfigFilePath, optional: true);
                    configBuilder.AddEnvironmentVariables(prefix: HardCoded.Config.EnvironmentVariablePrefix);
                    configBuilder.AddCommandLine(args);
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<ChillBotService>();
                })
                .UseConsoleLifetime();
        }
    }
}
