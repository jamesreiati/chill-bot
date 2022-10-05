using Discord;
using Discord.Interactions;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object representing a slash command.
    /// </summary>
    public class SlashCommand : IEntity<ulong>
    {
        /// <summary>
        /// The ID used for the slash command when the actual ID cannot be determined.
        /// </summary>
        public const ulong UnknownId = ulong.MinValue;

        /// <summary>
        /// Gets the unique ID of the slash command.
        /// </summary>
        public ulong Id { get; }

        /// <summary>
        /// Gets the representation of metadata information about the slash command.
        /// </summary>
        public SlashCommandInfo SlashCommandInfo { get; }

        /// <summary>
        /// Gets the text that can be used to link to the slash command within a Discord message.
        /// </summary>
        public string CommandLinkText => this.Id != SlashCommand.UnknownId ? $"</{this.SlashCommandInfo.Name}:{this.Id}>" : $"/{this.SlashCommandInfo.Name}";

        /// <summary>
        /// Constructs a new <see cref="SlashCommand"/>.
        /// </summary>
        /// <param name="id">The unique ID of the slash command.</param>
        /// <param name="slashCommandInfo">The metadata information for the slash command.</param>
        public SlashCommand(ulong id, SlashCommandInfo slashCommandInfo)
        {
            this.Id = id;

            ValidateArg.IsNotNull(slashCommandInfo, nameof(slashCommandInfo));
            this.SlashCommandInfo = slashCommandInfo;
        }

        /// <summary>
        /// Constructs a new <see cref="SlashCommand"/> with an unknown ID (<see cref="SlashCommand.UnknownId"/>).
        /// </summary>
        /// <param name="slashCommandInfo">The metadata information for the slash command.</param>
        public SlashCommand(SlashCommandInfo slashCommandInfo) : this(SlashCommand.UnknownId, slashCommandInfo)
        {
        }
    }
}
