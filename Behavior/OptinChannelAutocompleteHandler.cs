using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// Handles the auto-completion of opt-in channel names.
    /// </summary>
    public class OptinChannelAutocompleteHandler : AutocompleteHandler
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Cache for opt-in channel details.
        /// </summary>
        private IOptinChannelCacheManager optinChannelCache;

        /// <summary>
        /// Object pool of <see cref="OptinChannelCacheResult"/>s.
        /// </summary>
        private static ObjectPool<OptinChannelCacheResult> optinChannelCacheResultPool =
            new ObjectPool<OptinChannelCacheResult>(
                tFactory: () => new OptinChannelCacheResult(),
                preallocate: 3);

        public OptinChannelAutocompleteHandler(ILogger<OptinChannelAutocompleteHandler> logger, IGuildRepository guildRepository, IOptinChannelCacheManager optinChannelCache)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;

            ValidateArg.IsNotNull(optinChannelCache, nameof(optinChannelCache));
            this.optinChannelCache = optinChannelCache;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var optinChannelCacheResult = optinChannelCacheResultPool.Get();
            try
            {
                string userInput = autocompleteInteraction.Data.Current.Value.ToString();

                optinChannelCacheResult = await this.optinChannelCache.GetChannels(context.Guild as SocketGuild, optinChannelCacheResult);
                switch (optinChannelCacheResult.Result)
                {
                    case OptinChannelCacheResult.ResultType.Success:
                        IEnumerable<AutocompleteResult> results = optinChannelCacheResult.NamesDescriptions
                            .Where(x => x.name.StartsWith(userInput, StringComparison.InvariantCultureIgnoreCase)) // Only use results that start with user's input
                            .Select(r => new AutocompleteResult(r.name.Truncate(100), r.name));

                        // Return a maximum of 25 suggestions at a time (API limit)
                        return AutocompletionResult.FromSuccess(results.Take(25));

                    case OptinChannelCacheResult.ResultType.NoOptinCategory:
                        return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, "This server is not set up for opt-in channels.");

                    case OptinChannelCacheResult.ResultType.GuildDoesNotExist:
                        return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, "This server has not been configured for Chill Bot yet.");

                    case OptinChannelCacheResult.ResultType.GuildLocked:
                        return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, "Please try again.");

                    default:
                        return AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, optinChannelCacheResult.Result.ToString());
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Autocomplete Request dropped - exception thrown");
                return AutocompletionResult.FromError(e);
            }
            finally
            {
                optinChannelCacheResult.ClearReferences();
                optinChannelCacheResultPool.Return(optinChannelCacheResult);
            }
        }
    }
}
