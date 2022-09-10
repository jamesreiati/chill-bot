using Reiati.ChillBot.Commands;
using System.Collections.Generic;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// Responsible for managing data about how to command this bot.
    /// </summary>
    public class Help
    {
        /// <summary>
        /// All of the slash commands this bot recognizes via guild.
        /// </summary>
        private static CommandDetails[] guildSlashCommands = new[]
        {
            new CommandDetails(ListOptinCommand.CommandUsage, ListOptinCommand.CommandDescription),
            new CommandDetails(JoinOptinCommand.CommandUsage, JoinOptinCommand.CommandDescription),
            new CommandDetails(LeaveOptinCommand.CommandUsage, LeaveOptinCommand.CommandDescription),
            new CommandDetails(CreateOptinCommand.CommandUsage, CreateOptinCommand.CommandDescription),
            new CommandDetails(RenameOptinCommand.CommandUsage, RenameOptinCommand.CommandDescription),
            new CommandDetails(RedescribeOptinCommand.CommandUsage, RedescribeOptinCommand.CommandDescription),
        };

        /// <summary>
        /// All of the commands this bot recognizes via guild.
        /// </summary>
        private static CommandDetails[] guildCommands = new []
        {
            new CommandDetails(
                "list opt-ins",
                "Lists all of the opt-in channels on this server."),
            new CommandDetails(
                "join *channel-name*",
                "Adds you to the opt-in channel given."),
            new CommandDetails(
                "new opt-in *channel-name* *a helpful description of your channel*",
                "Creates a new opt-in channel."),
            new CommandDetails(
                "rename opt-in *current-channel-name* *new-channel-name*",
                "Changes the name of an existing opt-in channel."),
            new CommandDetails(
                "redescribe opt-in *channel-name* *a helpful description of the channel*",
                "Changes the description of an existing opt-in channel."),
        };

        /// <summary>
        /// All of the commands this bot recognizes via DM.
        /// </summary>
        private static CommandDetails[] dmCommands = new []
        {
            new CommandDetails(
                "leave opt-in *channel-name*",
                "Removes you from the channel given.")
        };

        /// <summary>
        /// All of the slash commands this bot recognizes via guild.
        /// </summary>
        public static IReadOnlyCollection<CommandDetails> GuildSlashCommands => guildSlashCommands;

        /// <summary>
        /// All of the commands this bot recognizes via guild.
        /// </summary>
        public static IReadOnlyCollection<CommandDetails> GuildCommands => guildCommands;

        /// <summary>
        /// All of the commands this bot recognizes via DM.
        /// </summary>
        public static IReadOnlyCollection<CommandDetails> DmCommands => dmCommands;

        /// <summary>
        /// Details about a command.
        /// </summary>
        public readonly struct CommandDetails
        {
            /// <summary>
            /// The text to trigger the command.
            /// </summary>
            public readonly string usage;

            /// <summary>
            /// A description of what the command does.
            /// </summary>
            public readonly string description;

            /// <summary>
            /// Constructs a new <see cref="CommandDetails"/>.
            /// </summary>
            /// <param name="usage">The text to trigger the command.</param>
            /// <param name="description">A description of what the command does.</param>
            public CommandDetails(string usage, string description)
            {
                this.usage = usage;
                this.description = description;
            }
        }
    }
}
