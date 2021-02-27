using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Engines
{
    /// <summary>
    /// Responsible for performing the welcome message.
    /// </summary>
    public class WelcomeMessageEngine
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILogger Logger = LogManager.GetLogger(typeof(WelcomeMessageEngine));

        /// <summary>
        /// Object pool of <see cref="FileBasedGuildRepository.CheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<FileBasedGuildRepository.CheckoutResult> checkoutResultPool =
            new ObjectPool<FileBasedGuildRepository.CheckoutResult>(
                tFactory: () => new FileBasedGuildRepository.CheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// Object pool of <see cref="System.Text.StringBuilder"/>s.
        /// </summary>
        private static ObjectPool<StringBuilder> welcomeMessageBuilderPool = new ObjectPool<StringBuilder>(
            tFactory: () => new StringBuilder(1024),
            preallocate: 3);

        /// <summary>
        /// Handle a user joined event.
        /// </summary>
        /// <param name="user">The user who joined.</param>
        /// <returns>When the task has finished.</returns>
        public async Task HandleUserJoin(SocketGuildUser user)
        {
            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await FileBasedGuildRepository.Instance.WaitForNotLockedCheckout(
                    user.Guild.Id,
                    HardCoded.Handlers.UserJoinLockedGuildTimeout,
                    checkoutResult);
                switch (checkoutResult.Result)
                {
                    case FileBasedGuildRepository.CheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            borrowedGuild.Commit = false;
                            var guild = borrowedGuild.Instance;

                            if (guild.WelcomeChannel.HasValue)
                            {
                                var welcomeChannel = user.Guild.GetTextChannel(
                                    guild.WelcomeChannel.GetValueOrDefault().Value);
                                
                                await welcomeChannel.SendMessageAsync(
                                    WelcomeMessageEngine.GetWelcomeMessage(user.Guild, guild, user.Id));
                            }
                        }
                    break;

                    case FileBasedGuildRepository.CheckoutResult.ResultType.DoesNotExist:
                    case FileBasedGuildRepository.CheckoutResult.ResultType.Locked:
                        // no-op: give up
                    break;

                    default:
                        throw new NotImplementedException(checkoutResult.Result.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Welcome dropped - exception thrown");
            }
            finally
            {
                checkoutResult.ClearReferences();
                checkoutResultPool.Return(checkoutResult);
            }
        }

        /// <summary>
        /// Gets the welcome message text.
        /// </summary>
        /// <param name="guildConnection">A connection to the guild.</param>
        /// <param name="guild">Guild data.</param>
        /// <param name="userId">The id representing the user.</param>
        /// <returns>The welcome message to send.</returns>
        private static string GetWelcomeMessage(SocketGuild guildConnection, Guild guild, Snowflake userId)
        {
            var builder = welcomeMessageBuilderPool.Get();
            builder.Clear();
            try
            {
                builder.AppendFormat("Hi <@{0}>!", userId);
                if (guild.OptinParentCategory.HasValue)
                {
                    var optinChannelCategoryId = guild.OptinParentCategory.GetValueOrDefault();
                    var optinChannelCategory = guildConnection.GetCategoryChannel(optinChannelCategoryId.Value);
                    if (optinChannelCategory.Channels.Count > 0)
                    {
                        builder.Append(" This server has a bunch of channels that you can join that you can't see right now, but you are welcome to join.");
                        foreach (var channel in optinChannelCategory.Channels)
                        {
                            builder.AppendFormat("\n | {0}", channel.Name);
                            // TODO: Add the channel description.
                        }
                        var exampleChannelName = optinChannelCategory.Channels.Last().Name;
                        builder.AppendFormat(
                            "\nLet me know if you're interested in any of them by sending me a message like, \"@Chill Bot join {0}\"",
                            exampleChannelName);
                    }
                }
                return builder.ToString();
            }
            finally
            {
                welcomeMessageBuilderPool.Return(builder);
            }
        }
    }
}
