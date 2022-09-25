using Discord.WebSocket;
using System;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of <typeparamref name="TItem"/> by guild.
    /// </summary>
    public interface IGuildCache<TItem>
    {
        /// <summary>
        /// Try to get the <typeparamref name="TItem"/> for a guild from the cache.
        /// </summary>
        /// <param name="guild">The guild whose <typeparamref name="TItem"/> should be retrieved from the cache.</param>
        /// <param name="value">The <typeparamref name="TItem"/> for the provided <paramref name="guild"/>, if it was present in the cache.</param>
        /// <returns>Whether the <typeparamref name="TItem"/> for the guild were present in the cache.</returns>
        bool TryGetValue(SocketGuild guild, out TItem value);

        /// <summary>
        /// Creates or overwrites the specified entry in the cache.
        /// </summary>
        /// <param name="guild">The guild whose <typeparamref name="TItem"/> should be set in the cache.</param>
        /// <param name="value">The <typeparamref name="TItem"/> to set in the cache for this guild.</param>
        /// <returns>The value that was set.</returns>
        TItem Set(SocketGuild guild, TItem value);

        /// <summary>
        /// Creates or overwrites the specified entry in the cache and sets the value with an absolute expiration date.
        /// </summary>
        /// <param name="guild">The guild whose <typeparamref name="TItem"/> should be set in the cache.</param>
        /// <param name="value">The <typeparamref name="TItem"/> to set in the cache for this guild.</param>
        /// <param name="absoluteExpirationRelativeToNow">The expiration time in absolute terms relative to the current time.</param>
        /// <returns>The value that was set.</returns>
        TItem Set(SocketGuild guild, TItem value, TimeSpan absoluteExpirationRelativeToNow);

        /// <summary>
        /// Removes the <typeparamref name="TItem"/> associated with the given guild from the cache.
        /// </summary>
        /// <param name="guild">The guild whose channels to remove from the cache.</param>
        void Remove(SocketGuild guild);
    }
}
