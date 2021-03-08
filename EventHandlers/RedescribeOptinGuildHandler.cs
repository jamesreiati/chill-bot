using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// Responsible for handling messages in a guild, attempting to update the description of opt-in channels.
    /// </summary>
    public class RedescribeOptinGuildHandler : AbstractRegexHandler
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILogger Logger = LogManager.GetLogger(typeof(RedescribeOptinGuildHandler));

        /// <summary>
        /// Object pool of <see cref="GuildCheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// The matcher for detecting the phrases:
        /// - <@123> redescribe opt-in {1} {2}
        /// - <@123> redescribe opt-in {1}
        /// - <@123> redescribe opt in {1} {2}
        /// - <@123> redescribe optin {1} {2}
        /// - <@123> re-describe opt-in {1} {2}
        /// - <@123> re describe opt-in {1} {2}
        /// And captures the channel name into a named capture group "channel", and the new description into a named capture group "description".
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*\<\@\!?\d+\>\s*re(?:-|\s)?describe\s+opt(?:-|\s)?in\s+(?<channel>\S+)\s*(?<description>.*)$",
            RegexOptions.IgnoreCase,
            HardCoded.Handlers.DefaultRegexTimeout);

        /// <summary>
        /// Emoji sent upon successful operation.
        /// </summary>
        private static readonly Emoji SuccessEmoji = new Emoji("✅");

        /// <summary>
        /// The repository of <see cref="Guild"/> objects.
        /// </summary>
        private IGuildRepository guildRepository;

        /// <summary>
        /// Constructs a <see cref="RedescribeOptinGuildHandler"/>.
        /// </summary>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public RedescribeOptinGuildHandler(IGuildRepository guildRepository)
            : base(RedescribeOptinGuildHandler.matcher)
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
            var messageReference = new MessageReference(message.Id, messageChannel.Id, guildConnection.Id);
            var channelName = handleCache.Groups["channel"].Captures[0].Value;
            var description = handleCache.Groups["description"].Captures[0].Value;

            if (string.IsNullOrEmpty(description))
            {
                await message.Channel.SendMessageAsync(
                    text: "The channel's description must be something meaningful. Ideally something that explains what it is.",
                    messageReference: messageReference);
                return;
            }

            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await this.guildRepository.Checkout(guildConnection.Id, checkoutResult);
                switch (checkoutResult.Result)
                {
                    case GuildCheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            var guildData = borrowedGuild.Instance;
                            var updateResult = await OptinChannel.UpdateDescription(
                                guildConnection: guildConnection,
                                guildData: guildData,
                                requestAuthor: author,
                                channelName: channelName,
                                description: description);
                            borrowedGuild.Commit = updateResult == OptinChannel.UpdateDescriptionResult.Success;

                            switch (updateResult)
                            {
                                case OptinChannel.UpdateDescriptionResult.Success:
                                    await message.AddReactionAsync(RedescribeOptinGuildHandler.SuccessEmoji);
                                    break;

                                case OptinChannel.UpdateDescriptionResult.NoPermissions:
                                    await message.Channel.SendMessageAsync(
                                        text: "You do not have permission to update the description of opt-in channels.",
                                        messageReference: messageReference);
                                    break;

                                case OptinChannel.UpdateDescriptionResult.NoOptinCategory:
                                    await message.Channel.SendMessageAsync(
                                        text: "This server is not set up for opt-in channels.",
                                        messageReference: messageReference);
                                    break;

                                case OptinChannel.UpdateDescriptionResult.NoSuchChannel:
                                    await message.Channel.SendMessageAsync(
                                        text: "An opt-in channel with this name does not exist.",
                                        messageReference: messageReference);
                                    break;

                                default:
                                    throw new NotImplementedException(updateResult.ToString());
                            }
                        }
                        break;

                    case GuildCheckoutResult.ResultType.DoesNotExist:
                        await message.Channel.SendMessageAsync(
                            text: "This server has not been configured for Chill Bot yet.",
                            messageReference: messageReference);
                        break;

                    case GuildCheckoutResult.ResultType.Locked:
                        await message.Channel.SendMessageAsync(
                            text: "Please try again.",
                            messageReference: messageReference);
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
                    messageReference: messageReference);
            }
            finally
            {
                checkoutResult.ClearReferences();
                checkoutResultPool.Return(checkoutResult);
            }
        }
    }
}
