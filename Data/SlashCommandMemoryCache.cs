using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of slash command information by guild.
    /// </summary>
    public class SlashCommandMemoryCache : GuildMemoryCache<IReadOnlyDictionary<SlashCommandInfo, SlashCommand>>, ISlashCommandCache
    {
        /// <summary>
        /// Constructs a <see cref="SlashCommandMemoryCache"/>
        /// </summary>
        /// <param name="memoryCache">The memory cache to use for caching slash commands.</param>
        public SlashCommandMemoryCache(IMemoryCache memoryCache) : base(memoryCache)
        {
        }

        /// <summary>
        /// Gets the key used to store the provided guild in the cache.
        /// </summary>
        /// <param name="guild">The guild whose key should be returned.</param>
        /// <returns>The key corresponding to the provided <paramref name="guild"/> in the cache.</returns>
        protected override string GetCacheKey(SocketGuild guild)
        {
            if (guild == null)
            {
                return default;
            }

            return nameof(SlashCommandMemoryCache) + "_" + guild.Id.ToString();
        }
    }
}
