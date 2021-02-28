namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// The result of a <see cref="IGuildRepository.Checkout(Snowflake)"/> call.
    /// </summary>
    /// <remarks>Designed to be poolable. Mimics the structure of a discriminated union.</remarks>
    public sealed class GuildCheckoutResult
    {
        /// <summary>
        /// The type of this result.
        /// </summary>
        public ResultType Result { get; private set; }

        /// <summary>
        /// [<see cref="ResultType.Success"/>] The <see cref="Data.Guild"/> associated with the given id.
        /// </summary>
        public Borrowed<Guild> BorrowedGuild { get; private set; }

        /// <summary>
        /// Set this result to the <see cref="ResultType.Success"/> type.
        /// </summary>
        /// <param name="borrowedGuild">The borrowed guild to return.</param>
        public void ToSuccess(Borrowed<Guild> borrowedGuild)
        {
            this.Result = ResultType.Success;
            this.BorrowedGuild = borrowedGuild;
        }

        /// <summary>
        /// Set this result to the <see cref="ResultType.DoesNotExist"/> type.
        /// </summary>
        public void ToDoesNotExist()
        {
            this.Result = ResultType.DoesNotExist;
        }

        /// <summary>
        /// Set this result to the <see cref="ResultType.Locked"/> type.
        /// </summary>
        public void ToLocked()
        {
            this.Result = ResultType.Locked;
        }

        /// <summary>
        /// Drops all references to objects.
        /// </summary>
        /// <remarks>Useful call before returning to a pool.</remarks>
        public void ClearReferences()
        {
            this.BorrowedGuild = null;
        }

        /// <summary>
        /// Result type of a <see cref="FileBasedGuildRepository.Checkout(Snowflake)"/> call.
        /// </summary>
        public enum ResultType
        {
            /// <summary>A <see cref="Data.Guild"/> was successfully checked out.</summary>
            Success,

            /// <summary>No guild was associated with the given guild id.</summary>
            DoesNotExist,

            /// <summary>This guild is currently in use, try again later.</summary>
            Locked,
        }
    }
}
