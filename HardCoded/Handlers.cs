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
    }
}
