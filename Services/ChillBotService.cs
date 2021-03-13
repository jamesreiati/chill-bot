using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Engines;
using Reiati.ChillBot.Tools;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Services
{
    /// <summary>
    /// The main service used to run Chill Bot.
    /// </summary>
    public class ChillBotService : IHostedService
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(ChillBotService));

        /// <summary>
        /// A cache of the mapping between Discord a log source and its corresponding log category.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> DiscordLogSourceCategoryCache = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Application configuration.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// Guild repository implementation.
        /// </summary>
        private readonly IGuildRepository guildRepository;

        /// <summary>
        /// The client used to connect to Discord.
        /// </summary>
        private DiscordShardedClient client;

        /// <summary>
        /// Constructs a new <see cref="ChillBotService"/>.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public ChillBotService(IConfiguration configuration, IGuildRepository guildRepository)
        {
            this.configuration = configuration;
            this.guildRepository = guildRepository;
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await InitializeClient().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (this.client != null)
            {
                await this.client.StopAsync().ConfigureAwait(false);
                this.client.Dispose();
                this.client = null;
            }
        }

        /// <summary>
        /// Initializes the client.
        /// </summary>
        /// <returns>When the client has been initialized and started.</returns>
        private async Task InitializeClient()
        {
            var config = new DiscordSocketConfig
            {
                TotalShards = 1,
                LogLevel = this.GetMinimumDiscordLogLevel().ToLogSeverity()
            };

            var client = new DiscordShardedClient(config);
            client.Log += ChillBotService.ForwardLogToLogging;
            client.ShardConnected += ChillBotService.ProcessShardConnected;

            var messageHandler = new CommandEngine(client, this.guildRepository);
            var userJoinedHandler = new WelcomeMessageEngine(this.guildRepository);

            client.MessageReceived += messageHandler.HandleMessageReceived;
            client.UserJoined += userJoinedHandler.HandleUserJoin;
            client.GuildAvailable += ChillBotService.BeginMembersDownload;

            string token = configuration[HardCoded.Config.DiscordTokenConfigKey];

            await client.LoginAsync(Discord.TokenType.Bot, token.Trim()).ConfigureAwait(false);
            await client.StartAsync().ConfigureAwait(false);
            this.client = client;
        }

        /// <summary>
        /// Handler for when a shard has connected.
        /// </summary>
        /// <param name="shard">The shard which is ready.</param>
        /// <returns>When the task has completed.</returns>
        private static Task ProcessShardConnected(DiscordSocketClient shard)
        {
            Logger.LogInformation("Shard connected;{{shardId:{shardId}}}", shard.ShardId);

            // Process any guild tasks that were waiting for the shard connection
            foreach (var guild in shard.Guilds)
            {
                // Begin the member download for any guilds that don't have all of the members downloaded
                if (!guild.HasAllMembers)
                {
                    var ignoreAwait = ChillBotService.BeginMembersDownload(guild);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Forwards all client logs to the console.
        /// </summary>
        /// <param name="log">The client log.</param>
        /// <returns>When the task has completed.</returns>
        private static Task ForwardLogToLogging(Discord.LogMessage log)
        {
            var logCategory = ChillBotService.DiscordLogSourceCategoryCache.GetOrAdd(log.Source, (source) => $"{nameof(Discord)}.{source}");
            var discordLogger = LogManager.GetLogger(logCategory);

            if (ChillBotService.IsDiscordReconnect(log))
            {
                discordLogger.Log(
                    LogLevel.Information,
                    "Client log - Client reconnect;{{exceptionType:{exceptionType}}}",
                    log.Exception?.GetType());
            }
            else
            {
                discordLogger.Log(
                    log.Severity.ToLogLevel(),
                    log.Exception,
                    "Client log - {logMessage}",
                    log.Message ?? string.Empty);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Whether or not a discord log represents a reconnect to the Discord service.
        /// </summary>
        /// <param name="log">The client log.</param>
        /// <returns>True if the log represents a reconnect, false otherwise.</returns>
        private static bool IsDiscordReconnect(Discord.LogMessage log)
        {
            var exception = log.Exception;
            if (exception is Discord.WebSocket.GatewayReconnectException)
            {
                return true;
            }
            else if (exception is System.Net.WebSockets.WebSocketException webSocketException)
            {
                if (webSocketException.WebSocketErrorCode == System.Net.WebSockets.WebSocketError.ConnectionClosedPrematurely)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Starts the task to download the guild's users into the client's cache.
        /// Should be invoked upon GuildAvailable.
        /// </summary>
        /// <param name="guild">The guild made available.</param>
        /// <returns>When the task has completed.</returns>
        private static Task BeginMembersDownload(SocketGuild guild)
        {
            var ignoreAwait = guild.DownloadUsersAsync();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get the minimum Discord category log level configured for any <see cref="Microsoft.Extensions.Logging"/> provider.
        /// </summary>
        /// <returns>The minimum log level configured for Discord logs.</returns>
        private LogLevel GetMinimumDiscordLogLevel()
        {
            bool logLevelConfigured = false;
            LogLevel minimumDiscordLogLevel = LogLevel.None;
            foreach (var configSetting in this.configuration.GetSection(nameof(Microsoft.Extensions.Logging)).AsEnumerable())
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
    }
}
