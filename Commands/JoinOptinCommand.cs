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
    /// Responsible for handling commands in a guild, related to joining opt-in channels.
    /// </summary>
    public class JoinOptinCommand : OptinCommandBase
    {
        public const string CommandName = "join";
        public const string CommandDescription = "Adds you to the opt-in channel given.";
        public const string CommandParameters = "*channel-name*";
        public const string CommandUsage = $"/{JoinOptinCommand.CommandName} {JoinOptinCommand.CommandParameters}";

        /// <summary>
        /// A logger.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Constructs a <see cref="JoinOptinCommand"/>
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public JoinOptinCommand(ILogger<JoinOptinCommand> logger, IGuildRepository guildRepository) : base(guildRepository)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;
        }

        /// <summary>
        /// Slash command to join an opt-in channel.
        /// </summary>
        /// <param name="channelName">The name of the channel to join.</param>
        /// <returns>When the task has completed.</returns>
        [EnabledInDm(false)]
        [SlashCommand(JoinOptinCommand.CommandName, JoinOptinCommand.CommandDescription)]
        public async Task JoinSlashCommand(
            [Summary(description: "The name of the channel to join"), Autocomplete(typeof(OptinChannelAutocompleteHandler))] string channelName
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
                            var joinResult = await OptinChannel.Join(
                                guildConnection: this.Context.Guild,
                                guildData: guildData,
                                requestAuthor: this.Context.User as SocketGuildUser,
                                channelName: channelName);
                            borrowedGuild.Commit = joinResult == OptinChannel.JoinResult.Success;

                            switch (joinResult)
                            {
                                case OptinChannel.JoinResult.Success:
                                    await this.RespondAsync($"{JoinOptinCommand.SuccessEmoji} Welcome to {channelName}!")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.JoinResult.NoSuchChannel:
                                    await this.RespondAsync(
                                        text: "An opt-in channel with this name does not exist.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.JoinResult.NoOptinCategory:
                                    await this.RespondAsync(
                                        text: "This server is not set up for opt-in channels.")
                                        .ConfigureAwait(false);
                                    break;

                                case OptinChannel.JoinResult.RoleMissing:
                                    await this.RespondAsync(
                                        text: "The role for this channel went missing. Talk to your server admin.")
                                        .ConfigureAwait(false);
                                    break;

                                default:
                                    throw new NotImplementedException(joinResult.ToString());
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
                            text: "Please try again.",
                            ephemeral: true)
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
    }
}
