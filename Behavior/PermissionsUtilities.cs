using System;
using System.Linq;
using System.Collections.Generic;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// Helper methods for permissions.
    /// </summary>
    public static class PermissionsUtilities
    {
        /// <summary>
        /// Returns whether a user has at least one allowed role.
        /// </summary>
        /// <param name="userRoles">The user's roles.</param>
        /// <param name="allowedRoles">The allowed roles.</param>
        /// <returns>True if the user has at least one of the allowed roles, false otherwise.</returns>
        public static bool HasPermission(IEnumerable<Snowflake> userRoles, IEnumerable<Snowflake> allowedRoles)
        {
            return userRoles.Any(x => allowedRoles.Contains(x));
        }
    }
}
