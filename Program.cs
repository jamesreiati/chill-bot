using System;
using System.IO;
using log4net;
using log4net.Config;

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
        private static ILog logger;

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
            
            Program.logger.Debug("test");
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
            Program.logger = LogManager.GetLogger("Bootstrap");
            Program.logger.InfoFormat("Logging configuration: {0}", configFile.FullName);
            return true;
        }
    }
}
