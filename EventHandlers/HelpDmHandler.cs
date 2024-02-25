using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Reiati.ChillBot.Behavior;
using Discord;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// Responsible for handling messages in a DM, asking for help
    /// </summary>
    public class HelpDmHandler : AbstractRegexHandler
    {
        /// <summary>
        /// The text to return when the help command is invoked.
        /// </summary>
        private static readonly string HelpMessage = HelpDmHandler.BuildHelpMessage();

        /// <summary>
        /// The matcher for detecting the phrases:
        /// - <@123> help
        /// - <@123> !help
        /// - <@123> --help
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*-{0,2}!?help\s*$",
            RegexOptions.IgnoreCase,
            HardCoded.Handlers.DefaultRegexTimeout);

        /// <summary>
        /// Constructs a <see cref="HelpDmHandler"/>.
        /// </summary>
        public HelpDmHandler()
            : base(HelpDmHandler.matcher)
        { }

        /// <summary>
        /// Implementers should derive from this to handle a matched message.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <param name="handleCache">The match object returned from the regex match.</param>
        /// <returns>The handle task.</returns>
        protected override async Task HandleMatchedMessage(IMessage message, Match handleCache)
        {
            await message.Channel.SendMessageAsync(HelpDmHandler.HelpMessage);
        }

        /// <summary>
        /// Builds the text to return when the help command is invoked.
        /// </summary>
        /// <returns>A description of the all the commands and what they do.</returns>
        private static string BuildHelpMessage()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("Here is every command I can respond to:");

            foreach(var command in Help.DmCommands)
            {
                builder.AppendFormat("\n| {0} - {1}", command.usage, command.description);
            }

            return builder.ToString();
        }
    }
}
