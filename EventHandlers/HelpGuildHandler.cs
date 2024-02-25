using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Reiati.ChillBot.Behavior;
using Discord;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// Responsible for handling messages in a guild, asking for help.
    /// </summary>
    public class HelpGuildHandler : AbstractRegexHandler
    {
        /// <summary>
        /// The text to return when the help command is invoked.
        /// </summary>
        private static readonly string HelpMessage = HelpGuildHandler.BuildHelpMessage();

        /// <summary>
        /// The matcher for detecting the phrases:
        /// - <@123> help
        /// - <@123> !help
        /// - <@123> --help
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*\<\@\!?\d+\>\s*-{0,2}!?help\s*$",
            RegexOptions.IgnoreCase,
            HardCoded.Handlers.DefaultRegexTimeout);

        /// <summary>
        /// Constructs a <see cref="HelpGuildHandler"/>.
        /// </summary>
        public HelpGuildHandler()
            : base(HelpGuildHandler.matcher)
        { }

        /// <summary>
        /// Implementers should derive from this to handle a matched message.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <param name="handleCache">The match object returned from the regex match.</param>
        /// <returns>The handle task.</returns>
        protected override async Task HandleMatchedMessage(IMessage message, Match handleCache)
        {
            var messageChannel = message.Channel as IGuildChannel;
            var messageReference = new MessageReference(message.Id, messageChannel.Id, messageChannel.Guild.Id);
            await message.Channel.SendMessageAsync(HelpGuildHandler.HelpMessage,
                messageReference: messageReference)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Builds the text to return when the help command is invoked.
        /// </summary>
        /// <returns>A description of the all the commands and what they do.</returns>
        private static string BuildHelpMessage()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("Here is every command I can respond to:");

            foreach(var command in Help.GuildCommands)
            {
                builder.AppendFormat("\n| {0} - {1}", command.usage, command.description);
            }

            builder.Append("\nThere are more commands I can respond to if you send me a direct message.");

            return builder.ToString();
        }
    }
}
