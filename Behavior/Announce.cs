using Discord.WebSocket;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// Responsible for managing behavior related to making announcements.
    /// </summary>
    public class Announce
    {
        /// <summary>
        /// Announces the creation of a channel in the given guild if an announcement channel is configured.
        /// </summary>
        /// <param name="guild">Information about the guild. May not be null.</param>
        /// <param name="requestAuthor">The author of the channel create request. May not be null.</param>
        /// <param name="channelName">The name of the new channel. May not be null.</param>
        /// <param name="channelDescription">The description of the new channel.</param>
        /// <returns>A task indicating completion.</returns>
        public static async Task ChannelCreation(
            Guild guild,
            SocketGuildUser requestAuthor,
            string channelName,
            string channelDescription)
        {
            ValidateArg.IsNotNullOrWhiteSpace(channelName, nameof(channelName));

            string message = $"<@{requestAuthor.Id}> created a new channel named **{channelName}**";
            if (!string.IsNullOrWhiteSpace(channelDescription))
            {
                message += $" with the description \"{channelDescription}\"";
            }
            message += $".\nLet me know if you're interested by using a command like, \"/join {channelName}\"";

            await Announce.SendAnnouncementMessageAsync(
                guildConnection: requestAuthor.Guild,
                guild: guild,
                message: message)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Announces the rename of a channel in the given guild if an announcement channel is configured.
        /// </summary>
        /// <param name="guild">Information about the guild. May not be null.</param>
        /// <param name="requestAuthor">The author of the channel rename request. May not be null.</param>
        /// <param name="oldChannelName">The old name of the channel that was renamed. May not be null.</param>
        /// <param name="newChannelName">The new name of the channel that was renamed. May not be null.</param>
        /// <returns>A task indicating completion.</returns>
        public static async Task ChannelRename(
            Guild guild,
            SocketGuildUser requestAuthor,
            string oldChannelName,
            string newChannelName)
        {
            ValidateArg.IsNotNullOrWhiteSpace(oldChannelName, nameof(oldChannelName));
            ValidateArg.IsNotNullOrWhiteSpace(newChannelName, nameof(newChannelName));

            await Announce.SendAnnouncementMessageAsync(
                guildConnection: requestAuthor.Guild,
                guild: guild,
                message: $"<@{requestAuthor.Id}> changed the name of the **{oldChannelName}** channel to **{newChannelName}**.")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Announces a change to the description of a channel in the given guild if an announcement channel is configured.
        /// </summary>
        /// <param name="guild">Information about the guild. May not be null.</param>
        /// <param name="requestAuthor">The author of the channel redescribe request. May not be null.</param>
        /// <param name="channelName">The name of the channel being described. May not be null.</param>
        /// <param name="oldDescription">The old description of the channel.</param>
        /// <param name="newDescription">The new description of the channel.</param>
        /// <returns>A task indicating completion.</returns>
        public static async Task ChannelRedescribe(
            Guild guild,
            SocketGuildUser requestAuthor,
            string channelName,
            string oldDescription,
            string newDescription)
        {
            ValidateArg.IsNotNullOrWhiteSpace(channelName, nameof(channelName));

            string message = $"<@{requestAuthor.Id}> ";

            if (string.IsNullOrWhiteSpace(newDescription))
            {
                message += $"removed the description of the **{channelName}** channel.";

                if (!string.IsNullOrWhiteSpace(oldDescription))
                {
                    message += $" The previous description was \"{oldDescription}\"";
                }
            }
            else if (string.IsNullOrWhiteSpace(oldDescription))
            {
                message += $"added a description to the **{channelName}** channel: \"{newDescription}\"";
            }
            else
            {
                message += $"changed the description of the **{channelName}** channel from \"{oldDescription}\" to \"{newDescription}\"";
            }

            await Announce.SendAnnouncementMessageAsync(
                guildConnection: requestAuthor.Guild,
                guild: guild,
                message: message)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a message to the announcement channel if one is configured for the provided guild.
        /// </summary>
        /// <param name="guildConnection">The connection to the guild the announcement should be posted in. May not be null.</param>
        /// <param name="guild">Information about the guild. May not be null.</param>
        /// <param name="message">The announcement message to send.</param>
        /// <returns>A task indicating completion.</returns>
        private static async Task SendAnnouncementMessageAsync(SocketGuild guildConnection, Guild guild, string message)
        {
            if (TryGetAnnouncementChannel(guildConnection, guild, out SocketTextChannel announcementChannel))
            {
                await announcementChannel.SendMessageAsync(
                    text: message,
                    allowedMentions: Discord.AllowedMentions.None)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Try to get the announcement channel configured for the provided guild.
        /// </summary>
        /// <param name="guildConnection">The connection to the guild to search for the announcement channel. May not be null.</param>
        /// <param name="guild">Information about the guild. May not be null.</param>
        /// <param name="announcementChannel">The announcement channel, if one is configured and exists.</param>
        /// <returns>Whether the announcement channel was obtained for the provided guild.</returns>
        private static bool TryGetAnnouncementChannel(SocketGuild guildConnection, Guild guild, out SocketTextChannel announcementChannel)
        {
            announcementChannel = default;

            if (guild.AnnouncementChannel.HasValue)
            {
                try
                {
                    announcementChannel = guildConnection.GetTextChannel(guild.AnnouncementChannel.Value.Value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
