using Discord.WebSocket;
using System;
using System.Collections.Generic;
using static Reiati.ChillBot.Behavior.OptinChannel.ListResult;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of opt-in channels by guild.
    /// </summary>
    public interface IOptinChannelCache
    {
        /// <summary>
        /// Try to get the opt-in channels for a guild from the cache.
        /// </summary>
        /// <param name="guild">The guild whose opt-in channels should be retrieved from the cache.</param>
        /// <param name="value">The opt-in channels for the provided <paramref name="guild"/>, if they were present in the cache.</param>
        /// <returns>Whether the opt-in channels for the guild were present in the cache.</returns>
        bool TryGetValue(SocketGuild guild, out IEnumerable<NameDescription> value);

        /// <summary>
        /// Creates or overwrites the specified entry in the cache.
        /// </summary>
        /// <param name="guild">The guild whose opt-in channels should be set in the cache.</param>
        /// <param name="value">The opt-in channels to set in the cache for this guild.</param>
        /// <returns>The value that was set.</returns>
        IEnumerable<NameDescription> Set(SocketGuild guild, IEnumerable<NameDescription> value);

        /// <summary>
        /// Creates or overwrites the specified entry in the cache and sets the value with an absolute expiration date.
        /// </summary>
        /// <param name="guild">The guild whose opt-in channels should be set in the cache.</param>
        /// <param name="value">The opt-in channels to set in the cache for this guild.</param>
        /// <param name="absoluteExpirationRelativeToNow">The expiration time in absolute terms realtive to the current time.</param>
        /// <returns>The value that was set.</returns>
        IEnumerable<NameDescription> Set(SocketGuild guild, IEnumerable<NameDescription> value, TimeSpan absoluteExpirationRelativeToNow);

        /// <summary>
        /// Removes the channels associated with the given guild from the cache.
        /// </summary>
        /// <param name="guild">The guild whose channels to remove from the cache.</param>
        void Remove(SocketGuild guild);
    }
}
