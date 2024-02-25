using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of slash commands by guild.
    /// </summary>
    public interface ISlashCommandCacheManager
    {
        /// <summary>
        /// Retrieves a dictionary of slash commands from the cache if present or queries the slash commands from Discord if they are not already cached.
        /// </summary>
        /// <param name="guild">The connection to the guild for querying slash commands.</param>
        /// <returns>The slash command information for the guild.</returns>
        Task<IReadOnlyCollection<SlashCommand>> GetAllSlashCommandsAsync(IGuild guild);

        /// <summary>
        /// Retrieves information about a specific slash command from the cache if present or queries the slash command from Discord if it is not already cached.
        /// </summary>
        /// <typeparam name="TModule">Declaring module type of this slash command, must be a type of <see cref="Discord.Interactions.InteractionModuleBase"/>.</typeparam>
        /// <param name="guild">The connection to the guild for querying slash commands.</param>
        /// <param name="methodName">Method name of the slash command handler, use of <see cref="nameof"/> is recommended.</param>
        /// <returns>The slash command information for the guild.</returns>
        Task<SlashCommand> GetSlashCommandInfoAsync<TModule>(IGuild guild, string methodName) where TModule : class;
    }
}
