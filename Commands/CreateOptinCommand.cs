using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using System;
using System.Threading.Tasks;
using ParameterSummaryAttribute = Discord.Interactions.SummaryAttribute;

namespace Reiati.ChillBot.Commands
{
    /// <summary>
    /// Responsible for handling commands in a guild, related to creating opt-in channels.
    /// </summary>
    public class CreateOptinCommand : OptinCommandBase
    {
        public const string CommandName = "create";
        public const string CommandDescription = "Creates a new opt-in channel.";
        public const string CommandParameters = "*channel-name* *a helpful description of your channel*";
        public const string CommandUsage = $"/{CreateOptinCommand.CommandName} {CreateOptinCommand.CommandParameters}";

        /// <summary>
        /// A logger.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Cache for opt-in channel details.
        /// </summary>
        private IOptinChannelCacheManager optinChannelCache;

        /// <summary>
        /// Cache for slash command details.
        /// </summary>
        private ISlashCommandCacheManager slashCommandCache;

        /// <summary>
        /// Constructs a <see cref="CreateOptinCommand"/>
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        /// <param name="optinChannelCache">The cache manager to use for opt-in channels.</param>
        /// <param name="slashCommandCache">Cache for slash command details.</param>
        public CreateOptinCommand(ILogger<CreateOptinCommand> logger, IGuildRepository guildRepository, IOptinChannelCacheManager optinChannelCache, ISlashCommandCacheManager slashCommandCache) : base(guildRepository)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;

            ValidateArg.IsNotNull(optinChannelCache, nameof(optinChannelCache));
            this.optinChannelCache = optinChannelCache;

            ValidateArg.IsNotNull(slashCommandCache, nameof(slashCommandCache));
            this.slashCommandCache = slashCommandCache;
        }

        /// <summary>
        /// Slash command to create a opt-in channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to create.</param>
        /// <param name="channelDescription">The description of the channel to create.</param>
        /// <returns>When the task has completed.</returns>
        [EnabledInDm(false)]
        [DefaultMemberPermissions(GuildPermission.ManageChannels)]
        [SlashCommand(CreateOptinCommand.CommandName, CreateOptinCommand.CommandDescription)]
        public async Task CreateSlashCommand(
            [ParameterSummary(description: "The name of the channel to create")] string channelName,
            [ParameterSummary(description: "The description of the channel to create. Ideally something that explains what it is.")] string channelDescription
            )
        {
            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await this.guildRepository.Checkout(this.Context.Guild.Id, checkoutResult).ConfigureAwait(false);
                switch (checkoutResult.Result)
                {
                    case GuildCheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            // Get the join slash command information
                            SlashCommand joinSlashCommand = await this.slashCommandCache.GetSlashCommandInfoAsync<JoinOptinCommand>(this.Context.Guild, nameof(JoinOptinCommand.JoinSlashCommand)).ConfigureAwait(false);

                            var guildData = borrowedGuild.Instance;
                            var createResult = await OptinChannel.Create(
                                guildConnection: this.Context.Guild,
                                guildData: guildData,
                                requestAuthor: this.Context.User as SocketGuildUser,
                                channelName: channelName,
                                description: channelDescription,
                                checkPermission: false,  // Slash commands can have permissions configured by the server admin, so do not perform our own permission check
                                joinCommandLink: joinSlashCommand?.CommandLinkText)
                                .ConfigureAwait(false);
                            borrowedGuild.Commit = createResult.ResultCode == OptinChannel.CreateResult.Success;

                            switch (createResult.ResultCode)
                            {
                                case OptinChannel.CreateResult.Success:
                                    // Clear the opt-in channel cache for this guild since a new opt-in channel was created
                                    this.optinChannelCache.ClearCache(this.Context.Guild);

                                    await this.RespondAsync($"{CreateOptinCommand.SuccessEmoji} New channel <#{createResult.ChannelId}> created. Enjoy!")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.CreateResult.NoPermissions:
                                    await this.RespondAsync("You do not have permission to create opt-in channels.",
                                        ephemeral: true)
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.CreateResult.NoOptinCategory:
                                    await this.RespondAsync("This server is not set up for opt-in channels.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.CreateResult.ChannelNameUsed:
                                    await this.RespondAsync("An opt-in channel with this name already exists.",
                                        ephemeral: true)
                                        .ConfigureAwait(false);
                                    break;

                                default:
                                    throw new NotImplementedException(createResult.ResultCode.ToString());
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
