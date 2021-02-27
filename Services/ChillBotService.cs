using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Engines;
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
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(ChillBotService));

        /// <summary>
        /// Application configuration.
        /// </summary>
        private readonly IConfiguration configuration;

        /// <summary>
        /// The client used to connect to Discord.
        /// </summary>
        private DiscordShardedClient client;

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
            client.Log += ChillBotService.ForwardLogToLogging;
            client.ShardConnected += ChillBotService.LogShardConnected;

            var messageHandler = new CommandEngine(client);
            var userJoinedHandler = new WelcomeMessageEngine();

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
        private static Task LogShardConnected(DiscordSocketClient shard)
        {
            Logger.LogInformation("Shard connected;{{shardId:{0}}}", shard.ShardId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Forwards all client logs to the console.
        /// </summary>
        /// <param name="log">The client log.</param>
        /// <returns>When the task has completed.</returns>
        private static Task ForwardLogToLogging(Discord.LogMessage log)
        {
            Logger.Log(log.Severity.ToLogLevel(), log.Exception, "Client log - {0}", log.Message ?? string.Empty);
            return Task.CompletedTask;
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
