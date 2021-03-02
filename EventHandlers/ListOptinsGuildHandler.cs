using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using Microsoft.Extensions.Logging;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// Responsible for handling messages in a guild, attempting to list opt-in channels.
    /// </summary>
    public class ListOptinsGuildHandler : AbstractRegexHandler
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILogger Logger = LogManager.GetLogger(typeof(ListOptinsGuildHandler));

        /// <summary>
        /// Object pool of <see cref="FileBasedGuildRepository.CheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// Object pool of <see cref="OptinChannel.ListResult"/>s.
        /// </summary>
        private static ObjectPool<OptinChannel.ListResult> listResultPool =
            new ObjectPool<OptinChannel.ListResult>(
                tFactory: () => new OptinChannel.ListResult(),
                preallocate: 3);

        /// <summary>
        /// Object pool of <see cref="System.Text.StringBuilder"/>s.
        /// </summary>
        private static ObjectPool<StringBuilder> welcomeMessageBuilderPool = new ObjectPool<StringBuilder>(
            tFactory: () => new StringBuilder(1024),
            preallocate: 3);

        /// <summary>
        /// The matcher for detecting the phrases:
        /// - <@123> list optins
        /// - <@123> list opt-ins
        /// - <@123> list opt ins
        /// - <@123> ls opt-ins
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*\<\@\!?\d+\>\s*li?st?\s+opt[\s-]?ins?\s*$",
            RegexOptions.IgnoreCase,
            HardCoded.Handlers.DefaultRegexTimeout);

        /// <summary>
        /// The repository of <see cref="Guild"/> objects.
        /// </summary>
        private IGuildRepository guildRepository;

        /// <summary>
        /// Constructs a <see cref="ListOptinsGuildHandler"/>
        /// </summary>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public ListOptinsGuildHandler(IGuildRepository guildRepository)
            : base(ListOptinsGuildHandler.matcher)
        {
            ValidateArg.IsNotNull(guildRepository, nameof(guildRepository));
            this.guildRepository = guildRepository;
        }

        /// <summary>
        /// Implementers should derive from this to handle a matched message.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <param name="handleCache">The match object returned from the regex match.</param>
        /// <returns>The handle task.</returns>
        protected override async Task HandleMatchedMessage(SocketMessage message, Match handleCache)
        {
            var messageChannel = message.Channel as SocketGuildChannel;
            var author = message.Author as SocketGuildUser;
            var guildConnection = messageChannel.Guild;

            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await this.guildRepository.Checkout(guildConnection.Id, checkoutResult);
                switch (checkoutResult.Result)
                {
                    case GuildCheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            borrowedGuild.Commit = false;
                            var guildData = borrowedGuild.Instance;

                            await ListOptinsGuildHandler.ListOptinChannels(message, guildConnection, guildData);
                        }
                    break;

                    case GuildCheckoutResult.ResultType.DoesNotExist:
                        await message.Channel.SendMessageAsync(
                            text: "This server has not been configured for Chill Bot yet.",
                            messageReference: message.Reference);
                    break;

                    case GuildCheckoutResult.ResultType.Locked:
                        await message.Channel.SendMessageAsync(
                            text: "Please try again.",
                            messageReference: message.Reference);
                    break;

                    default:
                        throw new NotImplementedException(checkoutResult.Result.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Request dropped - exception thrown");
                await message.Channel.SendMessageAsync(
                    text: "Something went wrong trying to do this for you. File a bug report with Chill Bot.",
                    messageReference: message.Reference);
            }
            finally
            {
                checkoutResult.ClearReferences();
                checkoutResultPool.Return(checkoutResult);
            }
        }

        /// <summary>
        /// Given guild data, list all of the optin channels.
        /// </summary>
        /// <param name="message">The source message (for replying to). May not be null.</param>
        /// <param name="guildConnection">A connection to the guild. May not be null.</param>
        /// <param name="guildData">The guild data. May not be null.</param>
        /// <returns>When listing has completed.</returns>
        private static async Task ListOptinChannels(SocketMessage message, SocketGuild guildConnection, Guild guildData)
        {
            var listResult = listResultPool.Get();
            try
            {
                listResult = OptinChannel.List(guildConnection, guildData, listResult);

                switch (listResult.Result)
                {
                    case OptinChannel.ListResult.ResultType.Success:
                        var namesDescriptions = listResult.NamesDescriptions;
                        if (namesDescriptions.Count > 0)
                        {
                            await message.Channel.SendMessageAsync(
                                ListOptinsGuildHandler.GetListingMessage(namesDescriptions));
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync(
                                text: "This server deosn't have any opt-in channels yet. Try creating one with \"@Chill Bot new opt-in channel-name A description of your channel!\"",
                                messageReference: message.Reference);
                        }
                    break;

                    case OptinChannel.ListResult.ResultType.NoOptinCategory:
                        await message.Channel.SendMessageAsync(
                            text: "This server is not set up for opt-in channels.",
                            messageReference: message.Reference);
                    break;

                    default:
                        throw new NotImplementedException(listResult.Result.ToString());
                }
            }
            finally
            {
                listResult.ClearReferences();
                listResultPool.Return(listResult);
            }
        }

        /// <summary>
        /// Builds a string which enumerates each of the channel names and descriptions.
        /// </summary>
        /// <param name="namesDescriptions">Some set of channel names and descriptions.</param>
        /// <returns>The string which describes them all.</returns>
        private static string GetListingMessage(IEnumerable<OptinChannel.ListResult.NameDescription> namesDescriptions)
        {
            var builder = welcomeMessageBuilderPool.Get();
            builder.Clear();
            try
            {
                builder.Append("The opt-in channels are:");
                var lastNameAdded = string.Empty;
                foreach (var nameDescription in namesDescriptions)
                {
                    builder.AppendFormat("\n | **{0}**", nameDescription.name);
                    lastNameAdded = nameDescription.name;
                    // TODO: Add the channel description.
                }
                builder.AppendFormat(
                    "\nLet me know if you're interested in any of them by sending me a message like, \"@Chill Bot join {0}\"",
                    lastNameAdded);
                return builder.ToString();
            }
            finally
            {
                welcomeMessageBuilderPool.Return(builder);
            }
        }
    }
}
