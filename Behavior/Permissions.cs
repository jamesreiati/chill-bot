using System;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// Responsible for managing behavior related to bot permissions. This is not Discord Roles/Permissions.
    /// </summary>
    public class Permissions
    {
        /// <summary>
        /// Whether the user has any permission to interact with this bot in the given guild.
        /// </summary>
        /// <param name="guild">An id representing a guild.</param>
        /// <param name="user">An id representing a user.</param>
        /// <returns>
        /// True if the user has any permission to interact with this bot in the given guild. False otherwise.
        /// </returns>
        public static bool HasAnyPermissions(Snowflake guild, Snowflake user)
        {
            // TODO: things we'd want to check
            //  - Is the guild being recognized
            //  - Does the guild have any opt-in channels
            //  - Does the user have create permissions
            return true;
        }
    }
}
