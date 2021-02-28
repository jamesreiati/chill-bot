namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// The type of a <see cref="IGuildRepository"/> implementation.
    /// </summary>
    public enum GuildRepositoryType
    {
        /// <summary>
        /// A file system based guild repository.
        /// </summary>
        File,

        /// <summary>
        /// An Azure Blob Storage based guild repository.
        /// </summary>
        AzureBlob
    }
}
