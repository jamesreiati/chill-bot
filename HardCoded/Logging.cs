using System;
using System.IO;

namespace Reiati.ChillBot.HardCoded
{
    /// <summary>
    /// Hard coded values related to logging.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// The path to the config file.
        /// </summary>
        public static readonly string ConfigFilePath = Path.Combine(".", "log4net.config.xml");
    }
}
