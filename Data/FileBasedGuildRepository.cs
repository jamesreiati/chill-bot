using System;
using System.IO;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// A repository of <see cref="Guild"/> objects to be checked out and checked in.
    /// </summary>
    public class FileBasedGuildRepository
    {
        /// <summary>
        /// HRResult of the exception when the file is already in use.
        /// </summary>
        private const int LockedFileHRResult = -2147024864;

        /// <summary>
        /// Path to the directory containing all of the guild's data.
        /// </summary>
        private static readonly string GuildsRepositoryPath = Path.Combine(".", "file-data", "guilds");

        /// <summary>
        /// The instance of this class.
        /// </summary>
        public static readonly FileBasedGuildRepository Instance = new FileBasedGuildRepository();

        /// <summary>
        /// Constructs a <see cref="FileBasedGuildRepository"/>.
        /// </summary>
        private FileBasedGuildRepository()
        {
            if (!Directory.Exists(FileBasedGuildRepository.GuildsRepositoryPath))
            {
                Directory.CreateDirectory(FileBasedGuildRepository.GuildsRepositoryPath);
            }
        }

        /// <summary>
        /// Checkout out or create a <see cref="Guild"/> if one does not exist.
        /// </summary>
        /// <param name="guildId">An id representing a guild.</param>
        /// <returns>The borrowed guild.</returns>
        public BorrowedGuild? TryCreateOrCheckout(Snowflake guildId)
        {
            try
            {
                var sourceStream = new FileStream(
                    FileBasedGuildRepository.GetFilePath(guildId),
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);

                var guild = new Guild();

                return new BorrowedGuild(
                    guild: guild,
                    data: sourceStream,
                    onReturn: FileBasedGuildRepository.ReturnGuild);
            }
            catch (IOException e)
            {
                if (e.HResult == FileBasedGuildRepository.LockedFileHRResult)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// The action invoked when a <see cref="BorrowedGuild"/> is being returned.
        /// </summary>
        /// <param name="guild">The guild that's being returned. May be null if the guild borrowed is null.</param>
        /// <param name="data">
        /// The object placed into the data field at the construction of the <see cref="BorrowedGuild"/>.
        /// </param>
        private static void ReturnGuild(Guild guild, object data)
        {
            var sourceStream = (FileStream)data;
            sourceStream.Dispose();
        }

        /// <summary>
        /// Returns the path to the file that would be created or has been created to store a Guild.
        /// </summary>
        /// <param name="guildId">An id representing a guild.</param>
        /// <returns>The path to the file that would be created or has been created to store a Guild.</returns>
        private static string GetFilePath(Snowflake guildId)
        {
            return Path.Combine(FileBasedGuildRepository.GuildsRepositoryPath, guildId + ".json");
        }
    }
}
