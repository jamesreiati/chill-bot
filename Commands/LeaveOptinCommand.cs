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
    /// Responsible for handling commands in a guild, related to leaving opt-in channels.
    /// </summary>
    public class LeaveOptinCommand : OptinCommandBase
    {
        public const string CommandName = "leave";
        public const string CommandDescription = "Removes you from the channel given.";
        public const string CommandParameters = "*channel-name*";
        public const string CommandUsage = $"/{LeaveOptinCommand.CommandName} {LeaveOptinCommand.CommandParameters}";

        /// <summary>
        /// A logger.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Constructs a <see cref="LeaveOptinCommand"/>
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public LeaveOptinCommand(ILogger<LeaveOptinCommand> logger, IGuildRepository guildRepository) : base(guildRepository)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;
        }

        /// <summary>
        /// Slash command to leave an opt-in channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to leave.</param>
        /// <returns>When the task has completed.</returns>
        /// <remarks>
        /// All responses from the leave command should be ephemeral (private) by default to allow the user to leave the channel silently (without informing other users) if they choose.
        /// </remarks>
        [EnabledInDm(false)]
        [SlashCommand(LeaveOptinCommand.CommandName, LeaveOptinCommand.CommandDescription)]
        public async Task Leave(
            [Summary(description: "The name of the channel to leave"), Autocomplete(typeof(OptinChannelAutocompleteHandler))] string channelName
            )
        {
            var checkoutResult = checkoutResultPool.Get();
            try
            {
                checkoutResult = await this.guildRepository.Checkout(this.Context.Guild.Id, checkoutResult)
                    .ConfigureAwait(false);
                switch (checkoutResult.Result)
                {
                    case GuildCheckoutResult.ResultType.Success:
                        using (var borrowedGuild = checkoutResult.BorrowedGuild)
                        {
                            borrowedGuild.Commit = false;
                            var guildData = borrowedGuild.Instance;

                            var leaveResult = await OptinChannel.Leave(
                                guildConnection: this.Context.Guild,
                                guildData: guildData,
                                requestAuthor: this.Context.User as SocketGuildUser,
                                channelName: channelName)
                                .ConfigureAwait(false);

                            switch (leaveResult)
                            {
                                case OptinChannel.LeaveResult.Success:
                                    await this.RespondAsync($"{LeaveOptinCommand.SuccessEmoji} You have left {channelName}.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.LeaveResult.NoSuchChannel:
                                    await this.RespondAsync("There is no opt-in channel with that name. Did you mean something else?")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.LeaveResult.NoOptinCategory:
                                    await this.RespondAsync("This server is not set up with any opt-in channels right now.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.LeaveResult.RoleMissing:
                                    await this.RespondAsync("This channel is not set up correctly. Contact the server admin.")
                                        .ConfigureAwait(false);
                                    break;

                                default:
                                    throw new NotImplementedException(leaveResult.ToString());
                            }
                        }
                        break;

                    case GuildCheckoutResult.ResultType.DoesNotExist:
                        await this.RespondAsync(
                            text: "This server has not been configured for Chill Bot yet.")
                            .ConfigureAwait(false);
                        break;

                    case GuildCheckoutResult.ResultType.Locked:
                        await this.RespondAsync(
                            text: "Please try again.")
                            .ConfigureAwait(false);
                        break;

                    default:
                        throw new NotImplementedException(checkoutResult.Result.ToString());
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
                checkoutResult.ClearReferences();
                checkoutResultPool.Return(checkoutResult);
            }
        }

        /// <summary>
        /// Override of the base <see cref="RespondAsync"/> method with the default value of <paramref name="ephemeral"/> changed to <c>true</c>.
        /// </summary>
        /// <returns>When the task has completed.</returns>
        /// <remarks>
        /// All responses from the leave command should be ephemeral (private) by default to allow the user to leave the channel silently (without informing other users) if they choose.
        /// </remarks>
        protected override async Task RespondAsync(string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = true, AllowedMentions allowedMentions = null, RequestOptions options = null, MessageComponent components = null, Embed embed = null)
        {
            await base.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, options, components, embed).ConfigureAwait(false);
        }
    }
}
