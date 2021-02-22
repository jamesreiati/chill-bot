using System;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// A guild which has been checked out or borrowed and must be returned.
    /// Return using <see cref="IDisposable.Dispose"/>.
    /// </summary>
    public readonly struct BorrowedGuild : IDisposable
    {
        /// <summary>
        /// Arbitrary data provided by the the lender/repository and provided back to it upon return.
        /// </summary>
        private readonly object data;

        /// <summary>
        /// Arbitrary action invoked upon return.
        /// </summary>
        private readonly Action<Guild, object> onReturn;

        /// <summary>
        /// The guild being borrowed.
        /// </summary>
        public readonly Guild guild;

        /// <summary>
        /// Constructs a <see cref="BorrowedGuild"/>.
        /// </summary>
        /// <param name="guild">The guild being borrowed.</param>
        /// <param name="data">
        /// Arbitrary data provided by the the lender/repository and provided back to it upon return.
        /// </param>
        /// <param name="onReturn">Arbitrary action invoked upon return.</param>
        public BorrowedGuild(Guild guild, object data, Action<Guild, object> onReturn)
        {
            this.guild = guild;
            this.data = data;
            this.onReturn = onReturn;
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            onReturn?.Invoke(this.guild, this.data);
        }
    }
}
