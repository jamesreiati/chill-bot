using System;
using System.Threading.Tasks;
using Reiati.ChillBot.Tools;
using Reiati.ChillBot.Data;
using Discord;
using Discord.WebSocket;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// Responsible for managing behavior related to the opt-in channels.
    /// <para>
    /// Opt-in channels are channels which are by default hidden from a user, but are supposed to be advertised to all
    /// users. By default, a user will not have the role associated with an opt-in channel, but any user may recieve
    /// the permissions to join that channel.</para>
    /// </summary>
    /// <remarks>
    /// Opt-in channels must be made under an opt-in category. This category *must* give the bot permisison to view all
    /// channels, and *should* deny all users from viewing all channels.
    /// </remarks>
    public class OptinChannel
    {
        /// <summary>
        /// Creates an opt-in channel in the given guild.
        /// </summary>
        /// <param name="guildConnection">
        /// The connection to the guild this channel is being created in. May not be null.
        /// </param>
        /// <param name="guildData">Information about this guild.</param>
        /// <param name="channelName">The requested name of the new channel.</param>
        /// <param name="description">The requested description of the new channel.</param>
        /// <returns>Whether or not the creation of the opt-in channel was successful.</returns>
        public static async Task<bool> TryCreate(
            SocketGuild guildConnection,
            Guild guildData,
            string channelName,
            string description)
        {
            if (!guildData.OptinParentCategory.HasValue)
            {
                return false;
            }
            var optinsCategory = guildData.OptinParentCategory.GetValueOrDefault();

            var createdTextChannel = await guildConnection.CreateTextChannelAsync(channelName, settings =>
            {
                settings.CategoryId = optinsCategory.Value;
                settings.Topic = description;
            });
            var createdRole = await guildConnection.CreateRoleAsync(
                name: OptinChannel.GetRoleName(createdTextChannel.Id),
                permissions: null,
                color: null,
                isHoisted: false,
                isMentionable: false);
            var newPermissions = createdTextChannel.AddPermissionOverwriteAsync(
                role: createdRole,
                permissions: new OverwritePermissions(viewChannel: PermValue.Allow));

            return true;
        }

        /// <summary>
        /// Returns the role name associated with a given opt-in channel.
        /// </summary>
        /// <param name="optinChannel">An id representing an opt-in channel. Not verified against real channels.</param>
        /// <returns>The name of the role to be (if not already) associated with a given opt-in channel.</returns>
        public static string GetRoleName(Snowflake optinChannel)
        {
            return "chill-" + optinChannel.Value;
        }
    }
}
