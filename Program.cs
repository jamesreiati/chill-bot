using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using log4net;
using log4net.Config;
using Reiati.ChillBot.EventHandlers;

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
        /// The client(s) used to connect to discord.
        /// </summary>
        private static DiscordShardedClient Client;

        /// <summary>
        /// Whether or not the listeners have already been attached to the client.
        /// </summary>
        private static bool ListenersAttached = false;

        /// <summary>
        /// A lock to prevent listener duplicate hooking.
        /// </summary>
        private static object ListenersLock = new object();

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
                    Program.Client?.StopAsync().GetAwaiter().GetResult();
                    Program.Client.Dispose();
                };

            try
            {
                Program.InitializeClient().GetAwaiter().GetResult();
                Task.Delay(Timeout.Infinite).GetAwaiter().GetResult();
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
        /// Initializes the clients.
        /// </summary>
        /// <returns>When the clients have been initialized and started.</returns>
        private static async Task InitializeClient()
        {
            var config = new DiscordSocketConfig
            {
                TotalShards = 1,
            };

            var client = new DiscordShardedClient(config);
            client.Log += LogAsyncFromClient;
            client.ShardConnected += ConnectedHandler;

            string token = await File.ReadAllTextAsync(HardCoded.Discord.TokenFilePath);

            await client.LoginAsync(Discord.TokenType.Bot, token.Trim());
            await client.StartAsync();
            Program.Client = client;
        }

        /// <summary>
        /// Handler for when a shard has connected.
        /// </summary>
        /// <param name="shard">The shard which is ready.</param>
        /// <returns>When the task has completed.</returns>
        private static Task ConnectedHandler(DiscordSocketClient shard)
        {
            Program.Logger.InfoFormat("Shard connected;{{shardId:{0}}}", shard.ShardId);
            Program.AttachListenersIfNeeded(shard);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Forwards all client logs to the console.
        /// </summary>
        /// <param name="log">The client log.</param>
        /// <returns>When the task has completed.</returns>
        private static Task LogAsyncFromClient(Discord.LogMessage log)
        {
            Program.Logger.InfoFormat("Client log - {0}", log.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fowards all shard logs to the console.
        /// </summary>
        /// <param name="log">The shard log.</param>
        /// <returns>When the task has completed.</returns>
        private static Task LogAsyncFromShard(Discord.LogMessage log)
        {
            Program.Logger.InfoFormat("Shard log - {0}", log.ToString());
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
        private static void AttachListenersIfNeeded(DiscordSocketClient shard)
        {
            if (!Program.ListenersAttached)
            {
                lock (Program.ListenersLock)
                {
                    if (!Program.ListenersAttached)
                    {
                        var messageHandler = new GeneralMessageHandler(shard.CurrentUser.Id);
                        var userJoinedHandler = new UserJoinedHandler();

                        shard.Log += LogAsyncFromShard;
                        shard.MessageReceived += messageHandler.HandleMessageReceived;
                        shard.UserJoined += userJoinedHandler.HandleUserJoin;
                        // TODO: shard.ReactionAdded
                        Program.ListenersAttached = true;
                    }
                }
            }
        }
    }
}
