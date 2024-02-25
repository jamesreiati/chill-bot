using Discord;
using Microsoft.Extensions.Caching.Memory;
using Reiati.ChillBot.Tools;
using System;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of <typeparamref name="TItem"/> by guild.
    /// </summary>
    public abstract class GuildMemoryCache<TItem> : IGuildCache<TItem>
    {
        /// <summary>
        /// Memory cache used for caching a <typeparamref name="TItem"/> per guild.
        /// </summary>
        protected IMemoryCache memoryCache;

        /// <summary>
        /// Constructs a <see cref="GuildMemoryCache<T>"/>
        /// </summary>
        /// <param name="memoryCache">The memory cache to use for caching a <typeparamref name="TItem"/> per guild.</param>
        public GuildMemoryCache(IMemoryCache memoryCache)
        {
            ValidateArg.IsNotNull(memoryCache, nameof(memoryCache));
            this.memoryCache = memoryCache;
        }

        /// <summary>
        /// Try to get the <typeparamref name="TItem"/> for a guild from the cache.
        /// </summary>
        /// <param name="guild">The guild whose <typeparamref name="TItem"/> should be retrieved from the cache.</param>
        /// <param name="value">The <typeparamref name="TItem"/> for the provided <paramref name="guild"/>, if it was present in the cache.</param>
        /// <returns>Whether the <typeparamref name="TItem"/> for the guild was present in the cache.</returns>
        public virtual bool TryGetValue(IGuild guild, out TItem value)
        {
            if (guild == null)
            {
                value = default;
                return false;
            }

            return this.memoryCache.TryGetValue(this.GetCacheKey(guild), out value);
        }

        /// <summary>
        /// Creates or overwrites the specified entry in the cache.
        /// </summary>
        /// <param name="guild">The guild whose <typeparamref name="TItem"/> should be set in the cache.</param>
        /// <param name="value">The <typeparamref name="TItem"/> to set in the cache for this guild.</param>
        /// <returns>The value that was set.</returns>
        public virtual TItem Set(IGuild guild, TItem value)
        {
            if (guild == null)
            {
                return default;
            }

            return this.memoryCache.Set(this.GetCacheKey(guild), value);
        }

        /// <summary>
        /// Creates or overwrites the specified entry in the cache and sets the value with an absolute expiration date.
        /// </summary>
        /// <param name="guild">The guild whose <typeparamref name="TItem"/> should be set in the cache.</param>
        /// <param name="value">The <typeparamref name="TItem"/> to set in the cache for this guild.</param>
        /// <param name="absoluteExpirationRelativeToNow">The expiration time in absolute terms relative to the current time.</param>
        /// <returns>The value that was set.</returns>
        public virtual TItem Set(IGuild guild, TItem value, TimeSpan absoluteExpirationRelativeToNow)
        {
            if (guild == null)
            {
                return default;
            }

            return this.memoryCache.Set(this.GetCacheKey(guild), value, absoluteExpirationRelativeToNow);
        }

        /// <summary>
        /// Removes the <typeparamref name="TItem"/> associated with the given guild from the cache.
        /// </summary>
        /// <param name="guild">The guild whose <typeparamref name="TItem"/> should be remove from the cache.</param>
        public virtual void Remove(IGuild guild)
        {
            if (guild == null)
            {
                return;
            }

            this.memoryCache.Remove(this.GetCacheKey(guild));
        }

        /// <summary>
        /// Gets the key used to store the provided guild in the cache.
        /// </summary>
        /// <param name="guild">The guild whose key should be returned.</param>
        /// <returns>The key corresponding to the provided <paramref name="guild"/> in the cache.</returns>
        protected abstract string GetCacheKey(IGuild guild);
    }
}
