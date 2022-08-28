using Discord.WebSocket;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// An object that manages a cache of opt-in channel listing results to be checked out and checked in.
    /// </summary>
    public interface IOptinChannelCacheManager
    {
        /// <summary>
        /// Retrieves a list of opt-in channels from the cache if present or queries the channels from Discord if they are not already cached.
        /// </summary>
        /// <param name="guild">The connection to the guild for querying opt-in channels.</param>
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>The borrowed opt-in channel list.</returns>
        Task<OptinChannelCacheResult> GetChannels(SocketGuild guild, OptinChannelCacheResult recycleResult);

        /// <summary>
        /// Removes the specified guild ID from the cache. It will need to be queried from Discord the next time it is requested.
        /// </summary>
        /// <param name="guild">The connection to the guild to remove from the cache.</param>
        void ClearCache(SocketGuild guild);
    }
}
