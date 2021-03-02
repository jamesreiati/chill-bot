using Reiati.ChillBot.Tools;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object representing a repository of <see cref="Guild"/> objects to be checked out and checked in.
    /// </summary>
    public interface IGuildRepository
    {
        /// <summary>
        /// Checkout out a <see cref="Guild"/>.
        /// </summary>
        /// <param name="guildId">An id representing a guild.</param>
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>The borrowed guild.</returns>
        Task<GuildCheckoutResult> Checkout(Snowflake guildId, GuildCheckoutResult recycleResult = null);
    }
}
