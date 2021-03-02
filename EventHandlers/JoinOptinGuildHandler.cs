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
    /// Responsible for handling messages in a guild, attempting to join opt-in channels.
    /// </summary>
    public class JoinOptinGuildHandler : AbstractRegexHandler
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILogger Logger = LogManager.GetLogger(typeof(JoinOptinGuildHandler));

        /// <summary>
        /// Object pool of <see cref="FileBasedGuildRepository.CheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// The matcher for detecting the phrases:
        /// - <@123> join {channel}
        /// - <@123> join #{channel}
        /// - <@123> join opt-in {channel}
        /// - <@123> join opt in {channel}
        /// - <@123> join optin {channel}
        /// And captures the proposed channel name into the named capture group "channel"
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*\<\@\!?\d+\>\s*join(\s+opt(?:-|\s)?in)?\s+#?(?<channel>\S+)$",
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
        /// Constructs a <see cref="JoinOptinGuildHandler"/>.
        /// </summary>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public JoinOptinGuildHandler(IGuildRepository guildRepository)
            : base(JoinOptinGuildHandler.matcher)
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
            var channelName = handleCache.Groups["channel"].Captures[0].Value;

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
                            var joinResult = await OptinChannel.Join(
                                guildConnection: guildConnection,
                                guildData: guildData,
                                requestAuthor: author,
                                channelName: channelName);
                            borrowedGuild.Commit = joinResult == OptinChannel.JoinResult.Success;

                            switch (joinResult)
                            {
                                case OptinChannel.JoinResult.Success:
                                    await message.AddReactionAsync(JoinOptinGuildHandler.SuccessEmoji);
                                break;

                                case OptinChannel.JoinResult.NoSuchChannel:
                                    await message.Channel.SendMessageAsync(
                                        text: "An opt-in channel with this name does not exist.",
                                        messageReference: message.Reference);
                                break;

                                case OptinChannel.JoinResult.NoOptinCategory:
                                    await message.Channel.SendMessageAsync(
                                        text: "This server is not set up for opt-in channels.",
                                        messageReference: message.Reference);
                                break;

                                case OptinChannel.JoinResult.RoleMissing:
                                    await message.Channel.SendMessageAsync(
                                        text: "The role for this channel went missing. Talk to your server admin.",
                                        messageReference: message.Reference);
                                break;

                                default:
                                    throw new NotImplementedException(joinResult.ToString());
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
    }
}
