using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Engines;
using Reiati.ChillBot.HardCoded;
using Reiati.ChillBot.Tools;
using System;
using System.Collections.Concurrent;
using System.Reflection;
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
        /// Application service provider.
        /// </summary>
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Guild repository implementation.
        /// </summary>
        private readonly IGuildRepository guildRepository;

        /// <summary>
        /// Cache for slash command details.
        /// </summary>
        private readonly ISlashCommandCacheManager slashCommandCache;

        /// <summary>
        /// The client used to connect to Discord.
        /// </summary>
        private DiscordShardedClient client;

        /// <summary>
        /// The service for registering Discord commands.
        /// </summary>
        private InteractionService interactionService;

        /// <summary>
        /// Constructs a new <see cref="ChillBotService"/>.
        /// </summary>
        /// <param name="client">The client used to connect to Discord.</param>
        /// <param name="interactionService">The service for registering Discord commands.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="serviceProvider">Application service provider.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        /// <param name="slashCommandCache">Cache for slash command details.</param>
        public ChillBotService(DiscordShardedClient client, InteractionService interactionService, IConfiguration configuration, IServiceProvider serviceProvider, IGuildRepository guildRepository, ISlashCommandCacheManager slashCommandCache)
        {
            this.client = client;
            this.interactionService = interactionService;
            this.configuration = configuration;
            this.serviceProvider = serviceProvider;
            this.guildRepository = guildRepository;
            this.slashCommandCache = slashCommandCache;
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

            if (this.interactionService != null)
            {
                this.interactionService.Dispose();
                this.interactionService = null;
            }
        }

        /// <summary>
        /// Initializes the client.
        /// </summary>
        /// <returns>When the client has been initialized and started.</returns>
        private async Task InitializeClient()
        {
            this.client.Log += ChillBotService.ForwardLogToLogging;
            this.client.ShardConnected += ChillBotService.ProcessShardConnected;
            this.client.ShardReady += this.ProcessShardReady;
            this.client.InteractionCreated += this.ProcessInteractionCreated;

            var messageHandler = new CommandEngine(client, this.guildRepository, this.slashCommandCache);
            var userJoinedHandler = new WelcomeMessageEngine(this.guildRepository, this.slashCommandCache);

            this.client.MessageReceived += messageHandler.HandleMessageReceived;
            this.client.UserJoined += userJoinedHandler.HandleUserJoin;
            this.client.GuildAvailable += ChillBotService.BeginMembersDownload;

            string token = configuration[HardCoded.Config.DiscordTokenConfigKey];

            await this.client.LoginAsync(Discord.TokenType.Bot, token.Trim()).ConfigureAwait(false);
            await this.client.StartAsync().ConfigureAwait(false);
        }

        private async Task InitializeModules()
        {
            await this.interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), this.serviceProvider).ConfigureAwait(false);

#if DEBUG
            string testGuildIdString = this.configuration[Config.TestGuildIdConfigKey];
            if (ulong.TryParse(testGuildIdString, out ulong testGuildId))
            {
                await this.interactionService.RegisterCommandsToGuildAsync(testGuildId).ConfigureAwait(false);
            }
            else
            {
                await this.interactionService.RegisterCommandsGloballyAsync().ConfigureAwait(false);
            }
#else
            await this.interactionService.RegisterCommandsGloballyAsync().ConfigureAwait(false);
#endif
        }

        /// <summary>
        /// Handler for when a shard has connected.
        /// </summary>
        /// <param name="shard">The shard which is ready.</param>
        /// <returns>When the task has completed.</returns>
        private static Task ProcessShardConnected(DiscordSocketClient shard)
        {
            Logger.LogInformation("Shard ready;{{shardId:{shardId}}}", shard.ShardId);

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
        /// Handler for when a shard is ready.
        /// </summary>
        /// <param name="shard">The shard which is ready.</param>
        /// <returns>When the task has completed.</returns>
        private async Task ProcessShardReady(DiscordSocketClient shard)
        {
            Logger.LogInformation("Shard connected;{{shardId:{shardId}}}", shard.ShardId);

            await InitializeModules().ConfigureAwait(false);
        }

        /// <summary>
        /// Handler for when an interaction is started.
        /// </summary>
        /// <param name="interaction">Information about the interaction.</param>
        /// <returns>When the task has completed.</returns>
        private async Task ProcessInteractionCreated(SocketInteraction interaction)
        {
            var context = new ShardedInteractionContext(this.client, interaction);
            await this.interactionService.ExecuteCommandAsync(context, this.serviceProvider).ConfigureAwait(false);
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
    }
}
