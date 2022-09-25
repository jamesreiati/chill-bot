using System.Collections.Generic;
using static Reiati.ChillBot.Behavior.OptinChannel.ListResult;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// An object that manages a cache of opt-in channels by guild.
    /// </summary>
    public interface IOptinChannelCache : IGuildCache<IEnumerable<NameDescription>>
    {
    }
}
