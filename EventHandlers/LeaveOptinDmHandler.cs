using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using log4net;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// Responsible for handling messages in a guild, attempting to list opt-in channels.
    /// </summary>
    public class LeaveOptinDmHandler : AbstractRegexHandler
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILog Logger = LogManager.GetLogger(typeof(LeaveOptinDmHandler));

        /// <summary>
        /// Object pool of <see cref="FileBasedGuildRepository.CheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<FileBasedGuildRepository.CheckoutResult> checkoutResultPool =
            new ObjectPool<FileBasedGuildRepository.CheckoutResult>(
                tFactory: () => new FileBasedGuildRepository.CheckoutResult(),
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
        /// And captures the proposed channel name into group 1
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*leave\s+opt(?:-|\s)?in\s+#?(\S+)$",
            RegexOptions.IgnoreCase,
            HardCoded.Handlers.DefaultRegexTimeout);

        /// <summary>
        /// Constructs a <see cref="LeaveOptinDmHandler"/>
        /// </summary>
        public LeaveOptinDmHandler()
            : base(LeaveOptinDmHandler.matcher)
        { }

        /// <summary>
        /// Implementers should derive from this to handle a matched message.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <param name="handleCache">The match object returned from the regex match.</param>
        /// <returns>The handle task.</returns>
        protected override async Task HandleMatchedMessage(SocketMessage message, Match handleCache)
        {
            var messageChannel = message.Channel as SocketDMChannel;
            var author = message.Author;
            var mutualGuilds = message.Author.MutualGuilds;

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
            var channelName = handleCache.Groups[1].Captures[0].Value;

            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await FileBasedGuildRepository.Instance.Checkout(guildConnection.Id, checkoutResult)
                    .ConfigureAwait(false);
                switch (checkoutResult.Result)
                {
                    case FileBasedGuildRepository.CheckoutResult.ResultType.Success:
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

                    case FileBasedGuildRepository.CheckoutResult.ResultType.DoesNotExist:
                        await message.Channel.SendMessageAsync(
                            text: "This server has not been configured for Chill Bot yet.",
                            messageReference: message.Reference)
                            .ConfigureAwait(false);
                    break;

                    case FileBasedGuildRepository.CheckoutResult.ResultType.Locked:
                        await message.Channel.SendMessageAsync(
                            text: "Please try again.",
                            messageReference: message.Reference)
                            .ConfigureAwait(false);
                    break;

                    default:
                        throw new NotImplementedException(checkoutResult.Result.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Request dropped - exception thrown;{{exception:{0}}}", e.ToString());
                await message.Channel.SendMessageAsync(
                    text: "Something went wrong trying to do this for you. File a bug report with Chill Bot.",
                    messageReference: message.Reference)
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
