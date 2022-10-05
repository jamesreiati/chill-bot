using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Commands;
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
        /// Object pool of <see cref="GuildCheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// Object pool of <see cref="System.Text.StringBuilder"/>s.
        /// </summary>
        private static ObjectPool<StringBuilder> welcomeMessageBuilderPool = new ObjectPool<StringBuilder>(
            tFactory: () => new StringBuilder(1024),
            preallocate: 3);

        /// <summary>
        /// The repository of <see cref="Guild"/> objects.
        /// </summary>
        private IGuildRepository guildRepository;

        /// <summary>
        /// Cache for slash command details.
        /// </summary>
        private ISlashCommandCacheManager slashCommandCache;

        /// <summary>
        /// Constructs a new <see cref="WelcomeMessageEngine"/>.
        /// </summary>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        /// <param name="slashCommandCache">Cache for slash command details.</param>
        public WelcomeMessageEngine(IGuildRepository guildRepository, ISlashCommandCacheManager slashCommandCache)
        {
            ValidateArg.IsNotNull(guildRepository, nameof(guildRepository));
            this.guildRepository = guildRepository;

            ValidateArg.IsNotNull(slashCommandCache, nameof(slashCommandCache));
            this.slashCommandCache = slashCommandCache;
        }

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
                checkoutResult = await this.guildRepository.WaitForNotLockedCheckout(
                    user.Guild.Id,
                    HardCoded.Handlers.UserJoinLockedGuildTimeout,
                    checkoutResult)
                    .ConfigureAwait(false);
                switch (checkoutResult.Result)
                {
                    case GuildCheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            borrowedGuild.Commit = false;
                            var guild = borrowedGuild.Instance;

                            if (guild.WelcomeChannel.HasValue)
                            {
                                var welcomeChannel = user.Guild.GetTextChannel(
                                    guild.WelcomeChannel.GetValueOrDefault().Value);
                                
                                await welcomeChannel.SendMessageAsync(
                                    await this.GetWelcomeMessage(user.Guild, guild, user.Id).ConfigureAwait(false))
                                    .ConfigureAwait(false);
                            }
                        }
                    break;

                    case GuildCheckoutResult.ResultType.DoesNotExist:
                    case GuildCheckoutResult.ResultType.Locked:
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
        private async Task<string> GetWelcomeMessage(SocketGuild guildConnection, Guild guild, Snowflake userId)
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

                            if (channel is SocketTextChannel textChannel && !string.IsNullOrEmpty(textChannel.Topic))
                            {
                                builder.AppendFormat(" - {0}", textChannel.Topic);
                            }
                        }

                        builder.Append("\nLet me know if you're interested in any of them by using a command like, \"");

                        // Get the join slash command information
                        SlashCommand joinSlashCommand = await this.slashCommandCache.GetSlashCommandInfoAsync<JoinOptinCommand>(guildConnection, nameof(JoinOptinCommand.JoinSlashCommand)).ConfigureAwait(false);
                        if (joinSlashCommand != null)
                        {
                            builder.Append(joinSlashCommand.CommandLinkText);
                        }
                        else
                        {
                            builder.Append('/');
                            builder.Append(JoinOptinCommand.CommandName);
                        }

                        var exampleChannelName = optinChannelCategory.Channels.Last().Name;
                        builder.AppendFormat(" {0}\"", exampleChannelName);
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
