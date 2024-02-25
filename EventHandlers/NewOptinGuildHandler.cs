using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Commands;

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
        /// Cache for slash command details.
        /// </summary>
        private ISlashCommandCacheManager slashCommandCache;

        /// <summary>
        /// Constructs a <see cref="NewOptinGuildHandler"/>.
        /// </summary>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        /// <param name="slashCommandCache">Cache for slash command details.</param>
        public NewOptinGuildHandler(IGuildRepository guildRepository, ISlashCommandCacheManager slashCommandCache)
            : base(NewOptinGuildHandler.matcher)
        {
            ValidateArg.IsNotNull(guildRepository, nameof(guildRepository));
            this.guildRepository = guildRepository;

            ValidateArg.IsNotNull(slashCommandCache, nameof(slashCommandCache));
            this.slashCommandCache = slashCommandCache;
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
            var channelName = handleCache.Groups[1].Captures[0].Value;

            if (!NewOptinGuildHandler.TryGetSecondMatch(handleCache, out string description))
            {
                await message.Channel.SendMessageAsync(
                    text: "The new channel's description must be something meaningful. Ideally something that explains what it is.",
                    messageReference: messageReference)
                    .ConfigureAwait(false);
                return;
            }

            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await this.guildRepository.Checkout(guildConnection.Id, checkoutResult).ConfigureAwait(false);
                switch (checkoutResult.Result)
                {
                    case GuildCheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            // Get the join slash command information
                            SlashCommand joinSlashCommand = await this.slashCommandCache.GetSlashCommandInfoAsync<JoinOptinCommand>(guildConnection, nameof(JoinOptinCommand.JoinSlashCommand)).ConfigureAwait(false);

                            var guildData = borrowedGuild.Instance;
                            var createResult = await OptinChannel.Create(
                                guildConnection: guildConnection,
                                guildData: guildData,
                                requestAuthor: author,
                                channelName: channelName,
                                description: description,
                                joinCommandLink: joinSlashCommand?.CommandLinkText)
                                .ConfigureAwait(false);
                            borrowedGuild.Commit = createResult == OptinChannel.CreateResult.Success;

                            switch (createResult)
                            {
                                case OptinChannel.CreateResult.Success:
                                    await message.AddReactionAsync(NewOptinGuildHandler.SuccessEmoji)
                                        .ConfigureAwait(false);
                                break;

                                case OptinChannel.CreateResult.NoPermissions:
                                    await message.Channel.SendMessageAsync(
                                        text: "You do not have permission to create opt-in channels.",
                                        messageReference: messageReference)
                                        .ConfigureAwait(false);
                                break;

                                case OptinChannel.CreateResult.NoOptinCategory:
                                    await message.Channel.SendMessageAsync(
                                        text: "This server is not set up for opt-in channels.",
                                        messageReference: messageReference)
                                        .ConfigureAwait(false);
                                break;

                                case OptinChannel.CreateResult.ChannelNameUsed:
                                    await message.Channel.SendMessageAsync(
                                        text: "An opt-in channel with this name already exists.",
                                        messageReference: messageReference)
                                        .ConfigureAwait(false);
                                break;

                                default:
                                    throw new NotImplementedException(createResult.ToString());
                            }
                        }
                    break;

                    case GuildCheckoutResult.ResultType.DoesNotExist:
                        await message.Channel.SendMessageAsync(
                            text: "This server has not been configured for Chill Bot yet.",
                            messageReference: messageReference)
                            .ConfigureAwait(false);
                    break;

                    case GuildCheckoutResult.ResultType.Locked:
                        await message.Channel.SendMessageAsync(
                            text: "Please try again.",
                            messageReference: messageReference)
                            .ConfigureAwait(false);
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
                    messageReference: messageReference)
                    .ConfigureAwait(false);
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
