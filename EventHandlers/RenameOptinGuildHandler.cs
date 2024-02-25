using Discord;
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
    /// Responsible for handling messages in a guild, attempting to rename opt-in channels.
    /// </summary>
    public class RenameOptinGuildHandler : AbstractRegexHandler
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILogger Logger = LogManager.GetLogger(typeof(RenameOptinGuildHandler));

        /// <summary>
        /// Object pool of <see cref="GuildCheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// The matcher for detecting the phrases:
        /// - <@123> rename opt-in {1} {2}
        /// - <@123> rename opt-in {1}
        /// - <@123> rename opt in {1} {2}
        /// - <@123> rename optin {1} {2}
        /// And captures the current channel name into a named capture group "currentName", and the new channel name into a named capture group "newName".
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*\<\@\!?\d+\>\s*rename\s+opt(?:-|\s)?in\s+(?<currentName>\S+)\s*(?<newName>\S*)$",
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
        /// Constructs a <see cref="RenameOptinGuildHandler"/>.
        /// </summary>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public RenameOptinGuildHandler(IGuildRepository guildRepository)
            : base(RenameOptinGuildHandler.matcher)
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
        protected override async Task HandleMatchedMessage(IMessage message, Match handleCache)
        {
            var messageChannel = message.Channel as IGuildChannel;
            var author = message.Author as IGuildUser;
            var guildConnection = messageChannel.Guild;
            var messageReference = new MessageReference(message.Id, messageChannel.Id, guildConnection.Id);
            var currentChannelName = handleCache.Groups["currentName"].Captures[0].Value;
            var newChannelName = handleCache.Groups["newName"].Captures[0].Value;

            if (string.IsNullOrEmpty(newChannelName))
            {
                await message.Channel.SendMessageAsync(
                    text: "The channel's new name must be something meaningful.",
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
                            var renameResult = await OptinChannel.Rename(
                                guildConnection: guildConnection,
                                guildData: guildData,
                                requestAuthor: author,
                                currentChannelName: currentChannelName,
                                newChannelName: newChannelName);
                            borrowedGuild.Commit = renameResult == OptinChannel.RenameResult.Success;

                            switch (renameResult)
                            {
                                case OptinChannel.RenameResult.Success:
                                    await message.AddReactionAsync(RenameOptinGuildHandler.SuccessEmoji);
                                    break;

                                case OptinChannel.RenameResult.NoPermissions:
                                    await message.Channel.SendMessageAsync(
                                        text: "You do not have permission to rename opt-in channels.",
                                        messageReference: messageReference);
                                    break;

                                case OptinChannel.RenameResult.NoOptinCategory:
                                    await message.Channel.SendMessageAsync(
                                        text: "This server is not set up for opt-in channels.",
                                        messageReference: messageReference);
                                    break;

                                case OptinChannel.RenameResult.NoSuchChannel:
                                    await message.Channel.SendMessageAsync(
                                        text: "An opt-in channel with this name does not exist.",
                                        messageReference: messageReference);
                                    break;

                                case OptinChannel.RenameResult.NewChannelNameUsed:
                                    await message.Channel.SendMessageAsync(
                                        text: "An opt-in channel with this new name already exists.",
                                        messageReference: messageReference);
                                    break;

                                default:
                                    throw new NotImplementedException(renameResult.ToString());
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
