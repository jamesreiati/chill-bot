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
        /// The configuration key of the setting that controls the opt-in channel cache life time.
        /// </summary>
        public const string OptinChannelCacheLifeTimeConfigKey = "OptinChannelCacheLifeTimeInMinutes";

        /// <summary>
        /// The configuration key of the setting that contains the ID of a test guild to be used for debugging.
        /// </summary>
        public const string TestGuildIdConfigKey = "TestGuildId";

        /// <summary>
        /// The configuration key of the Application Insights instrumentation key setting.
        /// </summary>
        public const string ApplicationInsightsInstrumentationKeyConfigKey = "ApplicationInsights:InstrumentationKey";

        /// <summary>
        /// The configuration key of the guild repository type setting.
        /// </summary>
        public const string GuildRepositoryTypeConfigKey = "GuildRepository:Type";

        /// <summary>
        /// The format string for the configuration key of the guild repository connection string setting.
        /// </summary>
        public const string GuildRepositoryConnectionStringConfigKeyFormat = "GuildRepository:{0}:ConnectionString";

        /// <summary>
        /// The format string for the configuration key of the guild repository container setting.
        /// </summary>
        public const string GuildRepositoryContainerConfigKeyFormat = "GuildRepository:{0}:Container";

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
