using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using Microsoft.Extensions.Logging;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// Responsible for handling messages in a guild, attempting to create opt-in channels.
    /// </summary>
    public class NewOptinGuildHandler : AbstractRegexHandler
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILogger Logger = LogManager.GetLogger(typeof(NewOptinGuildHandler));

        /// <summary>
        /// Object pool of <see cref="FileBasedGuildRepository.CheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// The matcher for detecting the phrases:
        /// - <@123> new opt-in {1} {2}
        /// - <@123> new opt-in {1}
        /// - <@123> new opt in {1} {2}
        /// - <@123> new optin {1} {2}
        /// And captures the channel name into group 1, and the description {} into group 2.
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*\<\@\!?\d+\>\s*new\s+opt(?:-|\s)?in\s+(\S+)\s*(.*)$",
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
        /// Constructs a <see cref="NewOptinGuildHandler"/>.
        /// </summary>
        public NewOptinGuildHandler(IGuildRepository guildRepository)
            : base(NewOptinGuildHandler.matcher)
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
            var channelName = handleCache.Groups[1].Captures[0].Value;

            if (!NewOptinGuildHandler.TryGetSecondMatch(handleCache, out string description))
            {
                await message.Channel.SendMessageAsync(
                    text: "The new channel's description must be something meaningful. Ideally something that explains what it is.",
                    messageReference: message.Reference);
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
                            var createResult = await OptinChannel.Create(
                                guildConnection: guildConnection,
                                guildData: guildData,
                                requestAuthor: author,
                                channelName: channelName,
                                description: description);
                            borrowedGuild.Commit = createResult == OptinChannel.CreateResult.Success;

                            switch (createResult)
                            {
                                case OptinChannel.CreateResult.Success:
                                    await message.AddReactionAsync(NewOptinGuildHandler.SuccessEmoji);
                                break;

                                case OptinChannel.CreateResult.NoPermissions:
                                    await message.Channel.SendMessageAsync(
                                        text: "You do not have permission to create opt-in channels.",
                                        messageReference: message.Reference);
                                break;

                                case OptinChannel.CreateResult.NoOptinCategory:
                                    await message.Channel.SendMessageAsync(
                                        text: "This server is not set up for opt-in channels.",
                                        messageReference: message.Reference);
                                break;

                                case OptinChannel.CreateResult.ChannelNameUsed:
                                    await message.Channel.SendMessageAsync(
                                        text: "An opt-in channel with this name already exists.",
                                        messageReference: message.Reference);
                                break;

                                default:
                                    throw new NotImplementedException(createResult.ToString());
                            }
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
            catch(Exception e)
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
        /// Tries to get the second match, returns false if that match does not exist, or it is white space.
        /// </summary>
        /// <param name="match">Any regex match. May not be null.</param>
        /// <param name="contents">The contents of the match.</param>
        /// <returns>True if there was a second match, and its contents were not white space.</returns>
        private static bool TryGetSecondMatch(Match match, out string contents)
        {
            contents = null;
            if (match.Groups.Count < 3)
            {
                return false;
            }

            contents = match.Groups[2].Captures[0].Value;
            return !string.IsNullOrWhiteSpace(contents);
        }
    }
}
