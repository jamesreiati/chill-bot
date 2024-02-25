using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using System;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Commands
{
    /// <summary>
    /// Responsible for handling commands in a guild, related to renaming opt-in channels.
    /// </summary>
    public class RedescribeOptinCommand : OptinCommandBase
    {
        public const string CommandName = "redescribe";
        public const string CommandDescription = "Changes the description of an existing opt-in channel.";
        public const string CommandParameters = "*channel-name* *a helpful description of the channel*";
        public const string CommandUsage = $"/{RedescribeOptinCommand.CommandName} {RedescribeOptinCommand.CommandParameters}";

        /// <summary>
        /// A logger.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Cache for opt-in channel details.
        /// </summary>
        private IOptinChannelCacheManager optinChannelCache;

        /// <summary>
        /// Constructs a <see cref="RedescribeOptinCommand"/>
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        /// <param name="optinChannelCache">The cache manager to use for opt-in channels.</param>
        public RedescribeOptinCommand(ILogger<RedescribeOptinCommand> logger, IGuildRepository guildRepository, IOptinChannelCacheManager optinChannelCache) : base(guildRepository)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;

            ValidateArg.IsNotNull(optinChannelCache, nameof(optinChannelCache));
            this.optinChannelCache = optinChannelCache;
        }

        [EnabledInDm(false)]
        [DefaultMemberPermissions(GuildPermission.ManageChannels)]
        [SlashCommand(RedescribeOptinCommand.CommandName, RedescribeOptinCommand.CommandDescription)]
        public async Task RedescribeSlashCommand(
            [Summary(description: "The name of the channel to redescribe"), Autocomplete(typeof(OptinChannelAutocompleteHandler))] string channelName,
            [Summary(description: "The new description of the channel. Ideally something that explains what it is.")] string newChannelDescription
            )
        {
            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await this.guildRepository.Checkout(this.Context.Guild.Id, checkoutResult);
                switch (checkoutResult.Result)
                {
                    case GuildCheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            var guildData = borrowedGuild.Instance;
                            var updateResult = await OptinChannel.UpdateDescription(
                                guildConnection: this.Context.Guild,
                                guildData: guildData,
                                requestAuthor: this.Context.User as IGuildUser,
                                channelName: channelName,
                                description: newChannelDescription,
                                checkPermission: false); // Slash commands can have permissions configured by the server admin outside of the bot, so do not perform our own permission check

                            borrowedGuild.Commit = updateResult == OptinChannel.UpdateDescriptionResult.Success;

                            switch (updateResult)
                            {
                                case OptinChannel.UpdateDescriptionResult.Success:
                                    // Clear the opt-in channel cache for this guild since a opt-in channel was updated
                                    this.optinChannelCache.ClearCache(this.Context.Guild);

                                    await this.RespondAsync($"{RedescribeOptinCommand.SuccessEmoji} Channel description updated.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.UpdateDescriptionResult.NoPermissions:
                                    await this.RespondAsync("You do not have permission to update the description of opt-in channels.",
                                        ephemeral: true)
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.UpdateDescriptionResult.NoOptinCategory:
                                    await this.RespondAsync("This server is not set up for opt-in channels.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.UpdateDescriptionResult.NoSuchChannel:
                                    await this.RespondAsync("An opt-in channel with this name does not exist.")
                                        .ConfigureAwait(false);
                                    break;

                                default:
                                    throw new NotImplementedException(updateResult.ToString());
                            }
                        }
                        break;

                    case GuildCheckoutResult.ResultType.DoesNotExist:
                        await this.RespondAsync("This server has not been configured for Chill Bot yet.")
                            .ConfigureAwait(false);
                        break;

                    case GuildCheckoutResult.ResultType.Locked:
                        await this.RespondAsync("Please try again.")
                            .ConfigureAwait(false);
                        break;

                    default:
                        throw new NotImplementedException(checkoutResult.Result.ToString());
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Request dropped - exception thrown");
                await this.RespondAsync("Something went wrong trying to do this for you. File a bug report with Chill Bot.")
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
