using System;
using System.Linq;
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
    /// Responsible for handling messages in a guild, attempting to leave opt-in channels.
    /// </summary>
    public class LeaveOptinDmHandler : AbstractRegexHandler
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILogger Logger = LogManager.GetLogger(typeof(LeaveOptinDmHandler));

        /// <summary>
        /// Object pool of <see cref="FileBasedGuildRepository.CheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// Emoji sent upon successful operation.
        /// </summary>
        private static readonly Emoji SuccessEmoji = new Emoji("✅");

        /// <summary>
        /// The matcher for detecting the phrases:
        /// - leave opt-in {1}
        /// - leave opt-in #{1}
        /// - leave optin {1}
        /// - leave opt in {1}
        /// - <@123> leave opt-in #{1}
        /// And captures the proposed channel name into the named capture group "channel"
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*(\<\@\!?\d+\>\s*)?leave\s+opt(?:-|\s)?in\s+#?(?<channel>\S+)$",
            RegexOptions.IgnoreCase,
            HardCoded.Handlers.DefaultRegexTimeout);

        /// <summary>
        /// The repository of <see cref="Guild"/> objects.
        /// </summary>
        private IGuildRepository guildRepository;

        /// <summary>
        /// Constructs a <see cref="LeaveOptinDmHandler"/>
        /// </summary>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public LeaveOptinDmHandler(IGuildRepository guildRepository)
            : base(LeaveOptinDmHandler.matcher)
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
            var messageChannel = message.Channel as SocketDMChannel;
            var author = message.Author as SocketUser;
            var mutualGuilds = author.MutualGuilds;

            if (mutualGuilds.Count < 1)
            {
                await messageChannel.SendMessageAsync("We're not in any servers together.").ConfigureAwait(false);
                return;
            }
            else if (mutualGuilds.Count > 1)
            {
                await messageChannel.SendMessageAsync("Sorry, right now I'm not programmed to handle DMs from people that I'm in multiple servers with.")
                    .ConfigureAwait(false);
                return;
            }

            var guildConnection = mutualGuilds.Single();//this.discordClient.GetGuild(mutualGuilds.Single().Value);
            var requestAuthor = guildConnection.GetUser(author.Id);
            var channelName = handleCache.Groups["channel"].Captures[0].Value;

            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await this.guildRepository.Checkout(guildConnection.Id, checkoutResult)
                    .ConfigureAwait(false);
                switch (checkoutResult.Result)
                {
                    case GuildCheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            borrowedGuild.Commit = false;
                            var guildData = borrowedGuild.Instance;

                            var leaveResult = await OptinChannel.Leave(
                                guildConnection: guildConnection,
                                guildData: guildData,
                                requestAuthor: requestAuthor,
                                channelName: channelName)
                                .ConfigureAwait(false);

                            switch (leaveResult)
                            {
                                case OptinChannel.LeaveResult.Success:
                                    await message.AddReactionAsync(LeaveOptinDmHandler.SuccessEmoji)
                                        .ConfigureAwait(false);
                                break;

                                case OptinChannel.LeaveResult.NoSuchChannel:
                                    await messageChannel.SendMessageAsync("There is no opt-in channel with that name. Did you mean something else?")
                                        .ConfigureAwait(false);
                                break;

                                case OptinChannel.LeaveResult.NoOptinCategory:
                                    await messageChannel.SendMessageAsync("This server is not set up with any opt-in channels right now.")
                                        .ConfigureAwait(false);
                                break;

                                case OptinChannel.LeaveResult.RoleMissing:
                                    await messageChannel.SendMessageAsync("This channel is not set up correctly. Contact the server admin.")
                                        .ConfigureAwait(false);
                                break;

                                default:
                                    throw new NotImplementedException(leaveResult.ToString());
                            }
                        }
                    break;

                    case GuildCheckoutResult.ResultType.DoesNotExist:
                        await message.Channel.SendMessageAsync(
                            text: "This server has not been configured for Chill Bot yet.")
                            .ConfigureAwait(false);
                    break;

                    case GuildCheckoutResult.ResultType.Locked:
                        await message.Channel.SendMessageAsync(
                            text: "Please try again.")
                            .ConfigureAwait(false);
                    break;

                    default:
                        throw new NotImplementedException(checkoutResult.Result.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Request dropped - exception thrown");
                await message.Channel.SendMessageAsync(
                    text: "Something went wrong trying to do this for you. File a bug report with Chill Bot.")
                    .ConfigureAwait(false);
            }
            finally
            {
                checkoutResult.ClearReferences();
                checkoutResultPool.Return(checkoutResult);
            }
        }
    }
}
