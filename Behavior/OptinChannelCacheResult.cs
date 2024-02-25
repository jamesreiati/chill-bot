using System.Collections.Generic;
using static Reiati.ChillBot.Behavior.OptinChannel.ListResult;

namespace Reiati.ChillBot.Behavior
{
    /// <summary>
    /// The result of a <see cref="OptinChannelCacheManager.GetChannels(Discord.IGuild)"/> call.
    /// </summary>
    public sealed class OptinChannelCacheResult
    {
        /// <summary>
        /// Underlying list.
        /// </summary>
        private readonly List<NameDescription> namesDescriptions = new List<NameDescription>(5);

        /// <summary>
        /// The type of this result.
        /// </summary>
        public ResultType Result { get; private set; }

        /// <summary>
        /// The names and descriptions of all the opt-in channels.
        /// </summary>
        public IReadOnlyList<NameDescription> NamesDescriptions => this.namesDescriptions;

        /// <summary>
        /// Set this result to the <see cref="ResultType.Success"/> type.
        /// </summary>
        /// <param name="namesDescriptions">Names and descriptions of channels.</param>
        public void ToSuccess(IEnumerable<NameDescription> namesDescriptions)
        {
            this.Result = ResultType.Success;
            this.namesDescriptions.Clear();
            this.namesDescriptions.AddRange(namesDescriptions);
        }

        /// <summary>
        /// Set this result to the <see cref="ResultType.NoOptinCategory"/> type.
        /// </summary>
        public void ToNoOptinCategory()
        {
            this.Result = ResultType.NoOptinCategory;
        }

        /// <summary>
        /// Set this result to the <see cref="ResultType.GuildDoesNotExist"/> type.
        /// </summary>
        public void ToGuildDoesNotExist()
        {
            this.Result = ResultType.GuildDoesNotExist;
        }

        /// <summary>
        /// Set this result to the <see cref="ResultType.GuildLocked"/> type.
        /// </summary>
        public void ToGuildLocked()
        {
            this.Result = ResultType.GuildLocked;
        }

        /// <summary>
        /// Drops all references to objects.
        /// </summary>
        /// <remarks>Useful call before returning to a pool.</remarks>
        public void ClearReferences()
        {
            this.namesDescriptions.Clear();
        }

        /// <summary>
        /// The result of a <see cref="OptinChannelCacheManager.GetChannels(Discord.IGuild)"/> call.
        /// </summary>
        public enum ResultType
        {
            /// <summary>The channels are able to be retrieved from the cache or Discord API.</summary>
            Success,

            /// <summary>The server has no Opt-in channel category.</summary>
            NoOptinCategory,

            /// <summary>No guild was associated with the given guild id.</summary>
            GuildDoesNotExist,

            /// <summary>This guild is currently in use, try again later.</summary>
            GuildLocked,
        }
    }
}
