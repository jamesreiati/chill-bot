using Discord.WebSocket;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Reiati.ChillBot.EventHandlers;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChillBotService));

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
        /// Constructs a new <see cref="ChillBotService"/>.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        public ChillBotService(IConfiguration configuration)
        {
            this.configuration = configuration;
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
            Logger.InfoFormat("Shard connected;{{shardId:{0}}}", shard.ShardId);
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
            Logger.InfoFormat("Client log - {0}", log.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fowards all shard logs to the console.
        /// </summary>
        /// <param name="log">The shard log.</param>
        /// <returns>When the task has completed.</returns>
        private Task LogAsyncFromShard(Discord.LogMessage log)
        {
            Logger.InfoFormat("Shard log - {0}", log.ToString());
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
                        var messageHandler = new GeneralMessageHandler(shard.CurrentUser.Id);
                        var userJoinedHandler = new UserJoinedHandler();

                        shard.Log += LogAsyncFromShard;
                        shard.MessageReceived += messageHandler.HandleMessageReceived;
                        shard.UserJoined += userJoinedHandler.HandleUserJoin;
                        // TODO: shard.ReactionAdded
                        this.listenersAttached = true;
                    }
                }
            }
        }
    }
}
