using System.IO;

namespace Reiati.ChillBot.HardCoded
{
    /// <summary>
    /// Hard coded values related to configuration.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// The prefix that can be used on environment variables to override configuration values.
        /// </summary>
        public const string EnvironmentVariablePrefix = "CHILLBOT_";

        /// <summary>
        /// The configuration key of the Discord token setting.
        /// </summary>
        public const string DiscordTokenConfigKey = "DiscordToken";

        /// <summary>
        /// The path to the default config file.
        /// </summary>
        public static readonly string DefaultConfigFilePath = Path.Combine(".", "config.json");

        /// <summary>
        /// The path to the local config file.
        /// </summary>
        public static readonly string LocalConfigFilePath = Path.Combine(".", "config.Local.json");
    }
}
