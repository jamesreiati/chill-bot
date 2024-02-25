using Discord;
using Discord.Interactions;
using Reiati.ChillBot.Behavior;
using Reiati.ChillBot.Data;
using Reiati.ChillBot.Tools;
using System.Text;

namespace Reiati.ChillBot.Commands
{
    /// <summary>
    /// Base class responsible for handling commands in a guild, related to opt-in channels.
    /// </summary>
    public abstract class OptinCommandBase : InteractionModuleBase<IInteractionContext>
    {
        #region Pools

        /// <summary>
        /// Object pool of <see cref="FileBasedGuildRepository.CheckoutResult"/>s.
        /// </summary>
        protected static ObjectPool<GuildCheckoutResult> checkoutResultPool =
            new ObjectPool<GuildCheckoutResult>(
                tFactory: () => new GuildCheckoutResult(),
                preallocate: 3);

        /// <summary>
        /// Object pool of <see cref="OptinChannel.ListResult"/>s.
        /// </summary>
        protected static ObjectPool<OptinChannel.ListResult> listResultPool =
            new ObjectPool<OptinChannel.ListResult>(
                tFactory: () => new OptinChannel.ListResult(),
                preallocate: 3);

        /// <summary>
        /// Object pool of <see cref="OptinChannelCacheResult"/>s.
        /// </summary>
        protected static ObjectPool<OptinChannelCacheResult> optinChannelCacheResultPool =
            new ObjectPool<OptinChannelCacheResult>(
                tFactory: () => new OptinChannelCacheResult(),
                preallocate: 3);

        /// <summary>
        /// Object pool of <see cref="System.Text.StringBuilder"/>s.
        /// </summary>
        protected static ObjectPool<StringBuilder> welcomeMessageBuilderPool = new ObjectPool<StringBuilder>(
            tFactory: () => new StringBuilder(1024),
            preallocate: 3);

        #endregion Pools

        /// <summary>
        /// Emoji sent upon successful operation.
        /// </summary>
        protected static readonly Emoji SuccessEmoji = new Emoji("✅");

        /// <summary>
        /// The repository of <see cref="Guild"/> objects.
        /// </summary>
        protected IGuildRepository guildRepository;

        /// <summary>
        /// Constructs a <see cref="OptinCommandBase"/>
        /// </summary>
        /// <param name="guildRepository">The repository used to read and write <see cref="Guild"/>s.</param>
        public OptinCommandBase(IGuildRepository guildRepository)
        {
            ValidateArg.IsNotNull(guildRepository, nameof(guildRepository));
            this.guildRepository = guildRepository;
        }
    }
}
