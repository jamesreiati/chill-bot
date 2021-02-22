using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using log4net;
using Reiati.ChillBot.Behavior;

namespace Reiati.ChillBot.EventHandlers
{
    /// <summary>
    /// Responsible for handling messages in a guild, attempting to create opt-in channels.
    /// </summary>
    public class NewOptinGuildHandler : AbstractRegexHandler
    {
        /// <summary>
        /// The matcher for detecting the phrases:
        /// - <@123> new opt-in {1} {2}
        /// - <@123> new opt-in {1}
        /// - <@123> new opt in {1} {2}
        /// - <@123> new optin {1} {2}
        /// And captures the channel name into group 1, and the description {} into group 2.
        /// </summary>
        private static Regex matcher = new Regex(
            @"^\s*\<\@\!?\d+\>\s*new\s+opt(?:-|\s)?in\s+(\S+)\s*(.*)$",
            RegexOptions.IgnoreCase,
            HardCoded.Handlers.DefaultRegexTimeout);

        /// <summary>
        /// Emoji sent upon successful operation.
        /// </summary>
        private static readonly Emoji SuccessEmoji = new Emoji("✅");

        /// <summary>
        /// A logger.
        /// </summary>
        private readonly ILog logger;

        /// <summary>
        /// Constructs a <see cref="NewOptinGuildHandler"/>.
        /// </summary>
        public NewOptinGuildHandler()
            : base(NewOptinGuildHandler.matcher)
        {
            this.logger = LogManager.GetLogger(typeof(NewOptinGuildHandler));
        }

        /// <summary>
        /// Implementers should derive from this to handle a matched message.
        /// </summary>
        /// <param name="message">The message received.</param>
        /// <param name="handleCache">The match object returned from the regex match.</param>
        /// <returns>The handle task.</returns>
        protected override async Task HandleMatchedMessage(SocketMessage message, Match handleCache)
        {
            var messageChannel = message.Channel as SocketGuildChannel;
            var channelName = handleCache.Groups[1].Captures[0].Value;

            if (!NewOptinGuildHandler.TryGetSecondMatch(handleCache, out string description))
            {
                await message.Channel.SendMessageAsync(
                    text: "The new channel's description must be something meaningful. Ideally something that explains what it is.",
                    messageReference: message.Reference);
                return;
            }

            var guild = messageChannel.Guild;

            var success = await OptinChannel.TryCreate(
                guild: guild,
                optinsCategory: HardCoded.Discord.OptInsCategory,
                channelName: channelName,
                description: description);

            if (success)
            {
                await message.AddReactionAsync(NewOptinGuildHandler.SuccessEmoji);
            }
            else
            {
                await message.Channel.SendMessageAsync(
                    text: "Something went wrong trying to do this for you. Contact your server admin for more help.",
                    messageReference: message.Reference);
            }
        }

        /// <summary>
        /// Tries to get the second match, returns false if that match does not exist, or it is white space.
        /// </summary>
        /// <param name="match">Any regex match. May not be null.</param>
        /// <param name="contents">The contents of the match.</param>
        /// <returns>True if there was a second match, and its contents were not white space.</returns>
        private static bool TryGetSecondMatch(Match match, out string contents)
        {
            contents = null;
            if (match.Groups.Count < 3)
            {
                return false;
            }

            contents = match.Groups[2].Captures[0].Value;
            return !string.IsNullOrWhiteSpace(contents);
        }
    }
}
