using System;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// A T which has been checked out or borrowed and must be returned.
    /// Return using <see cref="IDisposable.Dispose"/>.
    /// </summary>
    public class Borrowed<T> : IDisposable
    {
        /// <summary>
        /// Arbitrary data provided by the the lender/repository and provided back to it upon return.
        /// </summary>
        private readonly object data;

        /// <summary>
        /// Arbitrary action invoked upon return.
        /// </summary>
        private readonly Action<T, object, bool> onReturn;

        /// <summary>
        /// Constructs a <see cref="Borrowed<T>"/>.
        /// </summary>
        /// <param name="isntance">The T being borrowed.</param>
        /// <param name="data">
        /// Arbitrary data provided by the the lender/repository and provided back to it upon return.
        /// </param>
        /// <param name="onReturn">Arbitrary action invoked upon return.</param>
        public Borrowed(T isntance, object data, Action<T, object, bool> onReturn)
        {
            this.Instance = isntance;
            this.data = data;
            this.onReturn = onReturn;
        }

        /// <summary>
        /// The T being borrowed.
        /// </summary>
        public T Instance { get; }

        /// <summary>
        /// Whether or not to commit the changes on the object after return. (Default to true.)
        /// </summary>
        /// <value>True to commit, false to discard.</value>
        public bool Commit { get; set; } = true;

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            onReturn?.Invoke(this.Instance, this.data, this.Commit);
        }
    }
}
