using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.EventHandlers;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Engines
{
    /// <summary>
    /// Responsible for setting up command handlers and dispatching messages them.
    /// </summary>
    /// <remarks>
    /// None of the handlers may execute before the client has completely connected. This shouldn't be an issue though
    /// because the client shouldn't emit any events before it has completely connected.
    /// </remarks>
    public class CommandEngine
    {
        /// <summary>
        /// Object pool of <see cref="CanHandleResult"/>s.
        /// </summary>
        private static ObjectPool<CanHandleResult> handleResultPool = new ObjectPool<CanHandleResult>(
            tFactory: () => new CanHandleResult(),
            preallocate: 3);

        /// <summary>
        /// Emoji sent upon a message intended for the bot, which the bot could not handle.
        /// </summary>
        /// <returns></returns>
        private static readonly Emoji NoMatchEmoji = new Emoji("❓");

        /// <summary>
        /// A logger.
        /// </summary>
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CommandEngine));
        
        /// <summary>
        /// The id assigned to this bot.
        /// </summary>
        private readonly Lazy<Snowflake> botId;

        /// <summary>
        /// A discord client to make queries to.
        /// </summary>
        private readonly BaseSocketClient discordClient;

        /// <summary>
        /// A list of all the message handlers for direct messages.
        /// </summary>
        private readonly IReadOnlyList<IMessageHandler> dmHandlers;

        /// <summary>
        /// A list of all the message handlers for guild messages.
        /// </summary>
        private readonly IReadOnlyList<IMessageHandler> guildHandlers;

        /// <summary>
        /// The repository of <see cref="Guild"/> objects.
        /// </summary>
        private IGuildRepository guildRepository;

        /// <summary>
        /// Constructs a new <see cref="CommandEngine"/>.
        /// </summary>
        /// <param name="discordClient">A client to interact with discord. May not be null.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public CommandEngine(BaseSocketClient discordClient, IGuildRepository guildRepository)
        {
            ValidateArg.IsNotNull(discordClient, nameof(discordClient));
            ValidateArg.IsNotNull(guildRepository, nameof(guildRepository));
            this.discordClient = discordClient;
            this.guildRepository = guildRepository;
            this.botId = new Lazy<Snowflake>(this.GetBotId);

            this.dmHandlers = new List<IMessageHandler>()
            {
                new HelpDmHandler(),
                new LeaveOptinDmHandler(this.guildRepository),
            };
            this.guildHandlers = new List<IMessageHandler>()
            {
                new HelpGuildHandler(),
                new JoinOptinGuildHandler(this.guildRepository),
                new ListOptinsGuildHandler(this.guildRepository),
                new NewOptinGuildHandler(this.guildRepository),
            };
        }

        /// <summary>
        /// Helper method (for the Lazy field) to get the bot's id.
        /// </summary>
        /// <returns>An id representing the bot.</returns>
        private Snowflake GetBotId()
        {
            return this.discordClient.CurrentUser.Id;
        }

        /// <summary>
        /// The method to be subscribed to the client's MessageReceived event.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <returns>When the message has been handled.</returns>
        public async Task HandleMessageReceived(SocketMessage message)
        {
            if (message == null)
            {
                Logger.LogInformation("Message dropped - client emitted a null message");
                return;
            }

            if (message.Source != MessageSource.User)
            {
                // ignore non-user messages
                return;
            }

            var guildChannel = message.Channel as SocketGuildChannel;
            if (guildChannel != null)
            {
                await this.HandleGuildMessage(message).ConfigureAwait(false);
                return;
            }

            var dmChannel = message.Channel as SocketDMChannel;
            if (dmChannel != null)
            {
                await this.HandleDirectMessage(message).ConfigureAwait(false);
                return;
            }

            // ignore all messages from other channels
        }

        /// <summary>
        /// Handles a message sent in a guild.
        /// </summary>
        /// <param name="message">The message sent. May not be null.</param>
        /// <returns>When the message has been handled.</returns>
        private async Task HandleGuildMessage(SocketMessage message)
        {
            if (!message.MentionedUsers.Any(x => x.Id == this.botId.Value))
            {
                return;
            }

            await this.HandleMessage(message, this.guildHandlers).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles a message sent in a DM.
        /// </summary>
        /// <param name="message">The message sent. May not be null.</param>
        /// <returns>When the message has been handled.</returns>
        private async Task HandleDirectMessage(SocketMessage message)
        {
            if (message.Author.MutualGuilds.Count <= 0)
            {
                return;
            }

            await this.HandleMessage(message, this.dmHandlers).ConfigureAwait(false);
        }

        /// <summary>
        /// Submits the message to the first handler which can handle it. If none can, communicates to the user the
        /// message was not understood.
        /// </summary>
        /// <param name="message">The message from the user. May not be null.</param>
        /// <param name="handlers">Any list of handlers. May not be null.</param>
        /// <returns>When the message has been handled.</returns>
        private async Task HandleMessage(SocketMessage message, IReadOnlyList<IMessageHandler> handlers)
        {
            var handleResult = CommandEngine.handleResultPool.Get();
            try
            {
                bool wasHandled = false;

                for (int i = 0; i < handlers.Count && !wasHandled; i += 1)
                {
                    var handler = handlers[i];
                    var canHandle = await handler.CanHandleMessage(message, recycleResult: handleResult)
                        .ConfigureAwait(false);
                    switch (canHandle.Status)
                    {
                        case CanHandleResult.ResultStatus.Handleable:
                            wasHandled = true;
                            await handler.HandleMessage(message, canHandle.HandleCache).ConfigureAwait(false);
                        break;

                        case CanHandleResult.ResultStatus.TimedOut:
                            Logger.LogWarning(
                                "Handler timed out;{{handlerType:{handlerType},timeoutPeriod:{timeoutPeriod},message:{message}}}",
                                handler.GetType().Name,
                                canHandle.TimeOutPeriod,
                                message.Content);
                            continue;

                        case CanHandleResult.ResultStatus.Unhandleable:
                            continue;

                        default:
                            throw new NotImplementedException(canHandle.Status.ToString());
                    }
                }

                if (!wasHandled)
                {
                    await message.AddReactionAsync(NoMatchEmoji).ConfigureAwait(false);
                }
            }
            finally
            {
                handleResult.ClearReferences();
                CommandEngine.handleResultPool.Return(handleResult);
            }
        }
    }
}
