using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// Helper methods for <see cref="IGuildRepository"/>.
    /// </summary>
    public static class IGuildRepositoryExtensions
    {
        /// <summary>
        /// Keeps trying to checkout a guild (with backoff) until either maxTimeout has been hit,
        /// or a value has been returned that's not Locked.
        /// </summary>
        /// <param name="repository">A repository. May not be null.</param>
        /// <param name="guildId">An id associated with a guild.</param>
        /// <param name="maxTimeout">The maximum amount of time to spend waiting for this to unlock.</param>
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>The result.</returns>
        public static async Task<GuildCheckoutResult> WaitForNotLockedCheckout(
            this IGuildRepository repository,
            Snowflake guildId,
            TimeSpan maxTimeout,
            GuildCheckoutResult recycleResult = null)
        {
            Stopwatch timer = new Stopwatch();
            var retVal = recycleResult ?? new GuildCheckoutResult();
            TimeSpan nextDelay = TimeSpan.FromMilliseconds(1);

            timer.Start();
            retVal = await repository.Checkout(guildId, retVal);

            while (retVal.Result == GuildCheckoutResult.ResultType.Locked
                && timer.Elapsed + nextDelay < maxTimeout)
            {
                await Task.Delay(nextDelay);
                retVal = await repository.Checkout(guildId, retVal);
                nextDelay *= 1.5;
            }

            return retVal;
        }
    }
}
