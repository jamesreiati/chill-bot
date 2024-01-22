using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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
    public class RenameOptinCommand : OptinCommandBase
    {
        public const string CommandName = "rename";
        public const string CommandDescription = "Changes the name of an existing opt-in channel.";
        public const string CommandParameters = "*current-channel-name* *new-channel-name*";
        public const string CommandUsage = $"/{RenameOptinCommand.CommandName} {RenameOptinCommand.CommandParameters}";

        /// <summary>
        /// A logger.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Cache for opt-in channel details.
        /// </summary>
        private IOptinChannelCacheManager optinChannelCache;

        /// <summary>
        /// Constructs a <see cref="RenameOptinCommand"/>
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        /// <param name="optinChannelCache">The cache manager to use for opt-in channels.</param>
        public RenameOptinCommand(ILogger<RenameOptinCommand> logger, IGuildRepository guildRepository, IOptinChannelCacheManager optinChannelCache) : base(guildRepository)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;

            ValidateArg.IsNotNull(optinChannelCache, nameof(optinChannelCache));
            this.optinChannelCache = optinChannelCache;
        }

        [EnabledInDm(false)]
        [DefaultMemberPermissions(GuildPermission.ManageChannels)]
        [SlashCommand(RenameOptinCommand.CommandName, RenameOptinCommand.CommandDescription)]
        public async Task RenameSlashCommand(
            [Summary(description: "The current name of the channel to rename"), Autocomplete(typeof(OptinChannelAutocompleteHandler))] string currentChannelName,
            [Summary(description: "The new name of the channel")] string newChannelName
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
                            var renameResult = await OptinChannel.Rename(
                                guildConnection: this.Context.Guild,
                                guildData: guildData,
                                requestAuthor: this.Context.User as SocketGuildUser,
                                currentChannelName: currentChannelName,
                                newChannelName: newChannelName,
                                checkPermission: false); // Slash commands can have permissions configured by the server admin outside of the bot, so do not perform our own permission check

                            borrowedGuild.Commit = renameResult.ResultCode == OptinChannel.RenameResult.Success;

                            switch (renameResult.ResultCode)
                            {
                                case OptinChannel.RenameResult.Success:
                                    // Clear the opt-in channel cache for this guild since a opt-in channel was updated
                                    this.optinChannelCache.ClearCache(this.Context.Guild);

                                    await this.RespondAsync($"{RenameOptinCommand.SuccessEmoji} Channel <#{renameResult.ChannelId}> renamed.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.RenameResult.NoPermissions:
                                    await this.RespondAsync("You do not have permission to rename opt-in channels.",
                                        ephemeral: true)
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.RenameResult.NoOptinCategory:
                                    await this.RespondAsync("This server is not set up for opt-in channels.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.RenameResult.NoSuchChannel:
                                    await this.RespondAsync("An opt-in channel with this name does not exist.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.RenameResult.NewChannelNameUsed:
                                    await this.RespondAsync("An opt-in channel with this new name already exists.",
                                        ephemeral: true)
                                        .ConfigureAwait(false);
                                    break;

                                default:
                                    throw new NotImplementedException(renameResult.ResultCode.ToString());
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
