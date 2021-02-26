using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.EventHandlers;
using Reiati.ChillBot.Tools;
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
        private ILogger logger;

        /// <summary>
        /// Application configuration.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// The client used to connect to Discord.
        /// </summary>
        private DiscordShardedClient client;

        /// <summary>
        /// Whether the listeners have already been attached to the client.
        /// </summary>
        private bool listenersAttached = false;

        /// <summary>
        /// A lock to prevent listener duplicate hooking.
        /// </summary>
        private object listenersLock = new object();

        /// <summary>
        /// Dispatches messages to be handled.
        /// </summary>
        private IMessageDispatcher messageDispatcher;

        /// <summary>
        /// Handles user joined events.
        /// </summary>
        private IUserJoinedHandler userJoinedHandler;

        /// <summary>
        /// Constructs a new <see cref="ChillBotService"/>.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="logger">A logger.</param>
        /// <param name="messageDispatcher">The dispatcher for handling messages.</param>
        /// <param name="userJoinedHandler">The handler for user joined events.</param>
        public ChillBotService(IConfiguration configuration, ILogger<ChillBotService> logger, IMessageDispatcher messageDispatcher, IUserJoinedHandler userJoinedHandler)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.messageDispatcher = messageDispatcher;
            this.userJoinedHandler = userJoinedHandler;
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
            };

            var client = new DiscordShardedClient(config);
            client.Log += LogAsyncFromClient;
            client.ShardConnected += ConnectedHandler;

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
        private Task ConnectedHandler(DiscordSocketClient shard)
        {
            logger.LogInformation("Shard connected;{{shardId:{0}}}", shard.ShardId);
            AttachListenersIfNeeded(shard);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Forwards all client logs to the console.
        /// </summary>
        /// <param name="log">The client log.</param>
        /// <returns>When the task has completed.</returns>
        private Task LogAsyncFromClient(Discord.LogMessage log)
        {
            logger.Log(log.Severity.ToLogLevel(), log.Exception, "Client log - {0}", log.Message ?? string.Empty);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fowards all shard logs to the console.
        /// </summary>
        /// <param name="log">The shard log.</param>
        /// <returns>When the task has completed.</returns>
        private Task LogAsyncFromShard(Discord.LogMessage log)
        {
            logger.Log(log.Severity.ToLogLevel(), log.Exception, "Shard log - {0}", log.Message ?? string.Empty);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Creates and hooks the listeners to their respective events on all shards.
        /// </summary>
        /// <param name="shard">Any single shard.</param>
        /// <remarks>
        /// Events connected to 1 shard will be connected to all shards. If the listeners are attached once, they never
        /// have to be attached again.
        /// Source: https://github.com/discord-net/Discord.Net/blob/2.3.0/docs/faq/basics/client-basics.md#what-is-a-shardsharded-client-and-how-is-it-different-from-the-discordsocketclient
        /// </remarks>
        private void AttachListenersIfNeeded(DiscordSocketClient shard)
        {
            if (!this.listenersAttached)
            {
                lock (this.listenersLock)
                {
                    if (!this.listenersAttached)
                    {
                        this.messageDispatcher.RequireMention(shard.CurrentUser.Id);

                        shard.Log += LogAsyncFromShard;
                        shard.MessageReceived += this.messageDispatcher.HandleMessageReceived;
                        shard.UserJoined += this.userJoinedHandler.HandleUserJoin;
                        // TODO: shard.ReactionAdded
                        this.listenersAttached = true;
                    }
                }
            }
        }
    }
}
