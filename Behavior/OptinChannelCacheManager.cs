using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.HardCoded;
using Reiati.ChillBot.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using static Reiati.ChillBot.Behavior.OptinChannel.ListResult;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// An object that manages a cache of opt-in channel listing results to be checked out and checked in.
    /// </summary>
    public class OptinChannelCacheManager : IOptinChannelCacheManager
    {
        /// <summary>
        /// The amount of time channels should be retained in the cache.
        /// </summary>
        private readonly TimeSpan cacheLifeTime;

        /// <summary>
        /// A logger.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Cache for opt-in channel names
        /// </summary>
        private IOptinChannelCache optinChannelCache;

        /// <summary>
        /// The repository of <see cref="Guild"/> objects.
        /// </summary>
        private IGuildRepository guildRepository;

        /// <summary>
        /// Object pool of <see cref="FileBasedGuildRepository.CheckoutResult"/>s.
        /// </summary>
        private static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// Object pool of <see cref="OptinChannel.ListResult"/>s.
        /// </summary>
        private static ObjectPool<OptinChannel.ListResult> listResultPool =
            new ObjectPool<OptinChannel.ListResult>(
                tFactory: () => new OptinChannel.ListResult(),
                preallocate: 3);

        /// <summary>
        /// Constructs a <see cref="OptinChannelCacheManager"/>
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="optinChannelCache">The cache manager to use for opt-in channels.</param>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public OptinChannelCacheManager(ILogger<OptinChannelCacheManager> logger, IConfiguration configuration, IOptinChannelCache optinChannelCache, IGuildRepository guildRepository)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;

            ValidateArg.IsNotNull(configuration, nameof(configuration));
            cacheLifeTime = double.TryParse(configuration[Config.OptinChannelCacheLifeTimeConfigKey], out double lifeTime) ? TimeSpan.FromMinutes(lifeTime) : TimeSpan.FromMinutes(60);

            ValidateArg.IsNotNull(optinChannelCache, nameof(optinChannelCache));
            this.optinChannelCache = optinChannelCache;

            ValidateArg.IsNotNull(guildRepository, nameof(guildRepository));
            this.guildRepository = guildRepository;
        }

        /// <summary>
        /// Retrieves a list of opt-in channels from the cache if present or queries the channels from Discord if they are not already cached.
        /// </summary>
        /// <param name="guild">The connection to the guild for querying opt-in channels.</param>
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>The borrowed opt-in channel list.</returns>
        public async Task<OptinChannelCacheResult> GetChannels(IGuild guild, OptinChannelCacheResult recycleResult = null)
        {
            OptinChannelCacheResult retVal = recycleResult ?? new OptinChannelCacheResult();

            // Try to read from cache first
            if (!this.optinChannelCache.TryGetValue(guild, out IEnumerable<NameDescription> result))
            {
                result = Enumerable.Empty<NameDescription>();

                var checkoutResult = checkoutResultPool.Get();
                var listResult = listResultPool.Get();
                try
                {
                    checkoutResult = await this.guildRepository.Checkout(guild.Id, checkoutResult);
                    switch (checkoutResult.Result)
                    {
                        case GuildCheckoutResult.ResultType.Success:
                            using (var borrowedGuild = checkoutResult.BorrowedGuild)
                            {
                                borrowedGuild.Commit = false;
                                var guildData = borrowedGuild.Instance;

                                listResult = await OptinChannel.List(guild, guildData, listResult).ConfigureAwait(false);

                                switch (listResult.Result)
                                {
                                    case OptinChannel.ListResult.ResultType.Success:
                                        result = listResult.NamesDescriptions.ToImmutableArray();

                                        // Cache the results
                                        this.optinChannelCache.Set(guild, result, this.cacheLifeTime);
                                        break;

                                    case OptinChannel.ListResult.ResultType.NoOptinCategory:
                                        retVal.ToNoOptinCategory();
                                        return retVal;

                                    default:
                                        throw new NotImplementedException(listResult.Result.ToString());
                                }
                            }
                            break;

                        case GuildCheckoutResult.ResultType.DoesNotExist:
                            retVal.ToGuildDoesNotExist();
                            return retVal;

                        case GuildCheckoutResult.ResultType.Locked:
                            retVal.ToGuildLocked();
                            return retVal;

                        default:
                            throw new NotImplementedException(checkoutResult.Result.ToString());
                    }
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, "Cached channel request dropped - exception thrown");
                    throw;
                }
                finally
                {
                    checkoutResult.ClearReferences();
                    checkoutResultPool.Return(checkoutResult);

                    listResult.ClearReferences();
                    listResultPool.Return(listResult);
                }
            }

            retVal.ToSuccess(result);
            return retVal;
        }

        /// <summary>
        /// Removes the specified guild ID from the cache. It will need to be queried from Discord the next time it is requested.
        /// </summary>
        /// <param name="guild">The connection to the guild to remove from the cache.</param>
        public void ClearCache(IGuild guild)
        {
            this.optinChannelCache.Remove(guild);
        }
    }
}
