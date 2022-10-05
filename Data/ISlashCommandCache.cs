using Discord.Interactions;
using System.Collections.Generic;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of slash command information by guild.
    /// </summary>
    public interface ISlashCommandCache : IGuildCache<IReadOnlyDictionary<SlashCommandInfo, SlashCommand>>
    {
    }
}
