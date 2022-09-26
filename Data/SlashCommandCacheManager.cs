using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Reiati.ChillBot.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of slash commands by guild.
    /// </summary>
    public class SlashCommandCacheManager : ISlashCommandCacheManager
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// The client used to connect to Discord.
        /// </summary>
        private DiscordShardedClient client;

        /// <summary>
        /// The service for registering and reading Discord commands.
        /// </summary>
        private InteractionService interactionService;

        /// <summary>
        /// Cache of slash commands.
        /// </summary>
        private ISlashCommandCache slashCommandCache;

        /// <summary>
        /// /// Constructs a new <see cref="SlashCommandCacheManager"/>.
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="client">The client used to connect to Discord.</param>
        /// <param name="interactionService">The service for registering and reading Discord commands.</param>
        /// <param name="slashCommandCache">Cache of slash commands.</param>
        public SlashCommandCacheManager(ILogger<SlashCommandCacheManager> logger, DiscordShardedClient client, InteractionService interactionService, ISlashCommandCache slashCommandCache)
        {
            ValidateArg.IsNotNull(logger, nameof(logger));
            this.logger = logger;

            ValidateArg.IsNotNull(client, nameof(client));
            this.client = client;

            ValidateArg.IsNotNull(interactionService, nameof(interactionService));
            this.interactionService = interactionService;

            ValidateArg.IsNotNull(slashCommandCache, nameof(slashCommandCache));
            this.slashCommandCache = slashCommandCache;
        }

        /// <summary>
        /// Retrieves a dictionary of slash commands from the cache if present or queries the slash commands from Discord if they are not already cached.
        /// </summary>
        /// <param name="guild">The connection to the guild for querying slash commands.</param>
        /// <returns>The slash command information for the guild.</returns>
        public async Task<IReadOnlyCollection<SlashCommand>> GetAllSlashCommandsAsync(SocketGuild guild)
        {
            IReadOnlyDictionary<SlashCommandInfo, SlashCommand> commands = await this.GetSlashCommandDictionary(guild).ConfigureAwait(false);
            return new ReadOnlyCollection<SlashCommand>(commands.Values.ToList());
        }

        /// <summary>
        /// Retrieves information about a specific slash command from the cache if present or queries the slash command from Discord if it is not already cached.
        /// </summary>
        /// <typeparam name="TModule">Declaring module type of this slash command, must be a type of <see cref="Discord.Interactions.InteractionModuleBase"/>.</typeparam>
        /// <param name="guild">The connection to the guild for querying slash commands.</param>
        /// <param name="methodName">Method name of the slash command handler, use of <see cref="nameof"/> is recommended.</param>
        /// <returns>The slash command information for the guild.</returns>
        public async Task<SlashCommand> GetSlashCommandInfoAsync<TModule>(SocketGuild guild, string methodName) where TModule : class
        {
            try
            {
                SlashCommandInfo slashCommandInfo = this.interactionService.GetSlashCommandInfo<TModule>(methodName);

                IReadOnlyDictionary<SlashCommandInfo, SlashCommand> commands = await this.GetSlashCommandDictionary(guild).ConfigureAwait(false);
                if (!commands.TryGetValue(slashCommandInfo, out SlashCommand slashCommand))
                {
                    this.logger.LogWarning("Slash command not found for module {moduleType} and method {methodName} in guild {guildId}. Is it registered?", typeof(TModule).FullName, methodName, guild.Id);
                    
                    // Still return the slash command information despite it not being enriched with the guild-specific information
                    return new SlashCommand(slashCommandInfo);
                }

                return slashCommand;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error while getting slash command for module {moduleType} and method {methodName} in guild {guildId}.", typeof(TModule).FullName, methodName, guild.Id);
                return default;
            }
        }

        /// <summary>
        /// Gets the slash command dictionary from the cache or, if it is not present in the cache, constructs the slash command dictionary and stores it in the cache for future use.
        /// </summary>
        /// <param name="guild">The connection to the guild for querying slash commands.</param>
        /// <returns>The dictionary of slash command information for the guild.</returns>
        protected async Task<IReadOnlyDictionary<SlashCommandInfo, SlashCommand>> GetSlashCommandDictionary(SocketGuild guild)
        {
            try
            {
                if (!this.slashCommandCache.TryGetValue(guild, out IReadOnlyDictionary<SlashCommandInfo, SlashCommand> slashCommands))
                {
                    // Read all of the application commands from Discord
                    IReadOnlyCollection<RestGlobalCommand> restGlobalCommands = await this.client.Rest.GetGlobalApplicationCommands().ConfigureAwait(false);
                    IReadOnlyCollection<RestGuildCommand> restGuildCommands = await this.client.Rest.GetGuildApplicationCommands(guild.Id).ConfigureAwait(false);
                    List<RestApplicationCommand> allRestSlashCommands = restGlobalCommands.Cast<RestApplicationCommand>().Concat(restGuildCommands).Where(c => c.Type == ApplicationCommandType.Slash).ToList();

                    // Construct the dictionary of slash commands
                    var slashCommandDictionary = this.interactionService.SlashCommands.ToDictionary(
                        slashCommandInfo => slashCommandInfo,
                        slashCommandInfo => new SlashCommand(allRestSlashCommands.FirstOrDefault(restCommand => string.Equals(restCommand.Name, slashCommandInfo.Name, StringComparison.OrdinalIgnoreCase))?.Id ?? SlashCommand.UnknownId, slashCommandInfo));

                    slashCommands = new ReadOnlyDictionary<SlashCommandInfo, SlashCommand>(slashCommandDictionary);

                    // Store the dictionary of slash commands in the cache
                    this.slashCommandCache.Set(guild, slashCommands);
                }

                return slashCommands;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error reading or populating the slash command dictionary.");
                return this.interactionService.SlashCommands.ToDictionary(slashCommandInfo => slashCommandInfo, slashCommandInfo => new SlashCommand(slashCommandInfo));
            }
        }
    }
}
