using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using log4net;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// Responsible for dispatching messages to a handler, if there is one which can handle the message.
    /// </summary>
    public class GeneralMessageHandler
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GeneralMessageHandler));
        
        /// <summary>
        /// The id assigned to this bot.
        /// </summary>
        private readonly Snowflake botId;

        /// <summary>
        /// A list of all the message handlers for direct messages.
        /// </summary>
        private readonly IReadOnlyList<IMessageHandler> dmHandlers;

        /// <summary>
        /// A list of all the message handlers for guild messages.
        /// </summary>
        private readonly IReadOnlyList<IMessageHandler> guildHandlers;

        /// <summary>
        /// Constructs a new <see cref="GeneralMessageHandler"/>.
        /// </summary>
        /// <param name="botId">The id assigned to this bot.</param>
        public GeneralMessageHandler(Snowflake botId)
        {
            this.botId = botId;
            this.dmHandlers = new List<IMessageHandler>()
            {
            };
            this.guildHandlers = new List<IMessageHandler>()
            {
                new NewOptinGuildHandler(),
                new JoinOptinGuildHandler(),
                new ListOptinsGuildHandler(),
                new HelpGuildHandler(),
            };
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
                Logger.Info("Message dropped - client emitted a null message");
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
                await this.HandleGuildMessage(message);
                return;
            }

            var dmChannel = message.Channel as SocketDMChannel;
            if (dmChannel != null)
            {
                await this.HandleDirectMessage(message);
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
            if (!message.MentionedUsers.Any(x => x.Id == this.botId))
            {
                return;
            }

            await this.HandleMessage(message, this.guildHandlers);
        }

        /// <summary>
        /// Handles a message sent in a DM.
        /// </summary>
        /// <param name="message">The message sent. May not be null.</param>
        /// <returns>When the message has been handled.</returns>
        private async Task HandleDirectMessage(SocketMessage message)
        {
            await this.HandleMessage(message, this.dmHandlers);
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
            var handleResult = GeneralMessageHandler.handleResultPool.Get();
            try
            {
                bool wasHandled = false;

                for (int i = 0; i < handlers.Count && !wasHandled; i += 1)
                {
                    var handler = handlers[i];
                    var canHandle = await handler.CanHandleMessage(message, recycleResult: handleResult);
                    switch (canHandle.Status)
                    {
                        case CanHandleResult.ResultStatus.Handleable:
                            wasHandled = true;
                            await handler.HandleMessage(message, canHandle.HandleCache);
                        break;

                        case CanHandleResult.ResultStatus.TimedOut:
                            Logger.WarnFormat(
                                "Handler timed out;{{handlerType:{0},timeoutPeriod:{1},message:{2}}}",
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
                    await message.AddReactionAsync(NoMatchEmoji);
                }
            }
            finally
            {
                handleResult.ClearReferences();
                GeneralMessageHandler.handleResultPool.Return(handleResult);
            }
        }
    }
}
