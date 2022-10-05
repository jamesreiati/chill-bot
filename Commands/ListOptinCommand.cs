using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Commands
{
    /// <summary>
    /// Responsible for handling commands in a guild, related to listing opt-in channels.
    /// </summary>
    public partial class ListOptinCommand : OptinCommandBase
    {
        public const string CommandName = "list";
        public const string CommandDescription = "Lists all of the opt-in channels on this server.";
        public const string CommandParameters = "";
        public const string CommandUsage = $"/{ListOptinCommand.CommandName} {ListOptinCommand.CommandParameters}";

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
        /// Constructs a <see cref="ListOptinCommand"/>
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        /// <param name="optinChannelCache">The cache manager to use for opt-in channels.</param>
        /// <param name="slashCommandCache">Cache for slash command details.</param>
        public ListOptinCommand(ILogger<ListOptinCommand> logger, IGuildRepository guildRepository, IOptinChannelCacheManager optinChannelCache, ISlashCommandCacheManager slashCommandCache) : base(guildRepository)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;

            ValidateArg.IsNotNull(optinChannelCache, nameof(optinChannelCache));
            this.optinChannelCache = optinChannelCache;

            ValidateArg.IsNotNull(slashCommandCache, nameof(slashCommandCache));
            this.slashCommandCache = slashCommandCache;
        }

        /// <summary>
        /// Slash command to list available opt-in channels.
        /// </summary>
        /// <returns>When the task has completed.</returns>
        [EnabledInDm(false)]
        [SlashCommand(ListOptinCommand.CommandName, ListOptinCommand.CommandDescription)]
        public async Task List()
        {
            var optinChannelCacheResult = optinChannelCacheResultPool.Get();
            try
            {
                optinChannelCacheResult = await this.optinChannelCache.GetChannels(this.Context.Guild, optinChannelCacheResult).ConfigureAwait(false);
                switch (optinChannelCacheResult.Result)
                {
                    case OptinChannelCacheResult.ResultType.Success:
                        var namesDescriptions = optinChannelCacheResult.NamesDescriptions;
                        if (namesDescriptions.Count > 0)
                        {
                            await this.RespondAsync(
                                await this.GetListingMessage(namesDescriptions).ConfigureAwait(false))
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await this.RespondAsync(
                                text: "This server doesn't have any opt-in channels yet. Try creating one with \"/create channel-name A description of your channel!\"")
                                .ConfigureAwait(false);
                        }
                        break;

                    case OptinChannelCacheResult.ResultType.NoOptinCategory:
                        await this.RespondAsync(
                            text: "This server is not set up for opt-in channels.")
                            .ConfigureAwait(false);
                        break;

                    case OptinChannelCacheResult.ResultType.GuildDoesNotExist:
                        await this.RespondAsync(
                            text: "This server has not been configured for Chill Bot yet.")
                            .ConfigureAwait(false);
                        break;

                    case OptinChannelCacheResult.ResultType.GuildLocked:
                        await this.RespondAsync(
                            text: "Please try again.")
                            .ConfigureAwait(false);
                        break;

                    default:
                        throw new NotImplementedException(optinChannelCacheResult.Result.ToString());
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Request dropped - exception thrown");
                await this.RespondAsync(
                    text: "Something went wrong trying to do this for you. File a bug report with Chill Bot.")
                    .ConfigureAwait(false);
            }
            finally
            {
                optinChannelCacheResult.ClearReferences();
                optinChannelCacheResultPool.Return(optinChannelCacheResult);
            }
        }

        /// <summary>
        /// Builds a string which enumerates each of the channel names and descriptions.
        /// </summary>
        /// <param name="namesDescriptions">Some set of channel names and descriptions.</param>
        /// <returns>The string which describes them all.</returns>
        private async Task<string> GetListingMessage(IEnumerable<OptinChannel.ListResult.NameDescription> namesDescriptions)
        {
            var builder = welcomeMessageBuilderPool.Get();
            builder.Clear();
            try
            {
                builder.Append("The opt-in channels are:");
                var lastNameAdded = string.Empty;
                foreach (var nameDescription in namesDescriptions)
                {
                    builder.AppendFormat("\n | **{0}**", nameDescription.name);

                    if (!string.IsNullOrEmpty(nameDescription.description))
                    {
                        builder.AppendFormat(" - {0}", nameDescription.description);
                    }

                    lastNameAdded = nameDescription.name;
                }
                builder.Append("\nLet me know if you're interested in any of them by using a command like, \"");

                // Get the join slash command information
                SlashCommand joinSlashCommand = await this.slashCommandCache.GetSlashCommandInfoAsync<JoinOptinCommand>(this.Context.Guild, nameof(JoinOptinCommand.JoinSlashCommand)).ConfigureAwait(false);
                if (joinSlashCommand != null)
                {
                    builder.Append(joinSlashCommand.CommandLinkText);
                }
                else
                {
                    builder.Append('/');
                    builder.Append(JoinOptinCommand.CommandName);
                }

                builder.AppendFormat(" {0}\"", lastNameAdded);

                return builder.ToString();
            }
            finally
            {
                welcomeMessageBuilderPool.Return(builder);
            }
        }
    }
}
