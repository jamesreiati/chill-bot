using System;

namespace Reiati.ChillBot.HardCoded
{
    /// <summary>
    /// Hard coded values related to Handlers.
    /// </summary>
    public static class Handlers
    {
        /// <summary>
        /// The default amount of time a regex should be allowed to run before it times out.
        /// </summary>
        public static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromMilliseconds(2);

        /// <summary>
        /// The default amount of time the user join event should wait until it gives up on welcoming the user.
        /// </summary>
        public static readonly TimeSpan UserJoinLockedGuildTimeout = TimeSpan.FromMinutes(1);
    }
}
