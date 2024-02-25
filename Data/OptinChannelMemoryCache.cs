using Discord;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using static Reiati.ChillBot.Behavior.OptinChannel.ListResult;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of opt-in channels by guild.
    /// </summary>
    public class OptinChannelMemoryCache : GuildMemoryCache<IEnumerable<NameDescription>>, IOptinChannelCache
    {
        /// <summary>
        /// Constructs a <see cref="OptinChannelMemoryCache"/>
        /// </summary>
        /// <param name="memoryCache">The memory cache to use for caching opt-in channels.</param>
        public OptinChannelMemoryCache(IMemoryCache memoryCache) : base(memoryCache)
        {
        }

        /// <summary>
        /// Gets the key used to store the provided guild in the cache.
        /// </summary>
        /// <param name="guild">The guild whose key should be returned.</param>
        /// <returns>The key corresponding to the provided <paramref name="guild"/> in the cache.</returns>
        protected override string GetCacheKey(IGuild guild)
        {
            if (guild == null)
            {
                return default;
            }

            return nameof(OptinChannelMemoryCache) + "_" + guild.Id.ToString();
        }
    }
}
