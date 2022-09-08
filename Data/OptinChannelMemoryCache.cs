using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Reiati.ChillBot.Tools;
using System;
using System.Collections.Generic;
using static Reiati.ChillBot.Behavior.OptinChannel.ListResult;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of opt-in channels by guild.
    /// </summary>
    public class OptinChannelMemoryCache : IOptinChannelCache
    {
        /// <summary>
        /// Memory cache used for caching opt-in channel names
        /// </summary>
        private IMemoryCache memoryCache;

        /// <summary>
        /// Constructs a <see cref="OptinChannelMemoryCache"/>
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="memoryCache">The memory cache to use for caching opt-in channels.</param>
        public OptinChannelMemoryCache(IConfiguration configuration, IMemoryCache memoryCache)
        {
            ValidateArg.IsNotNull(memoryCache, nameof(memoryCache));
            this.memoryCache = memoryCache;
        }

        /// <summary>
        /// Try to get the opt-in channels for a guild from the cache.
        /// </summary>
        /// <param name="guild">The guild whose opt-in channels should be retrieved from the cache.</param>
        /// <param name="value">The opt-in channels for the provided <paramref name="guild"/>, if they were present in the cache.</param>
        /// <returns>Whether the opt-in channels for the guild were present in the cache.</returns>
        public bool TryGetValue(SocketGuild guild, out IEnumerable<NameDescription> value)
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
        /// <param name="guild">The guild whose opt-in channels should be set in the cache.</param>
        /// <param name="value">The opt-in channels to set in the cache for this guild.</param>
        /// <returns>The value that was set.</returns>
        public IEnumerable<NameDescription> Set(SocketGuild guild, IEnumerable<NameDescription> value)
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
        /// <param name="guild">The guild whose opt-in channels should be set in the cache.</param>
        /// <param name="value">The opt-in channels to set in the cache for this guild.</param>
        /// <param name="absoluteExpirationRelativeToNow">The expiration time in absolute terms relative to the current time.</param>
        /// <returns>The value that was set.</returns>
        public IEnumerable<NameDescription> Set(SocketGuild guild, IEnumerable<NameDescription> value, TimeSpan absoluteExpirationRelativeToNow)
        {
            if (guild == null)
            {
                return default;
            }

            return this.memoryCache.Set(this.GetCacheKey(guild), value, absoluteExpirationRelativeToNow);
        }

        /// <summary>
        /// Removes the channels associated with the given guild from the cache.
        /// </summary>
        /// <param name="guild">The guild whose channels to remove from the cache.</param>
        public void Remove(SocketGuild guild)
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
        protected string GetCacheKey(SocketGuild guild)
        {
            if (guild == null)
            {
                return default;
            }

            return nameof(OptinChannelMemoryCache) + "_" + guild.Id.ToString();
        }
    }
}
