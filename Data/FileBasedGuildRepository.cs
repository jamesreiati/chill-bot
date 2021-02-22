using System;
using System.IO;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public async Task<BorrowedGuild?> TryCheckout(Snowflake guildId)
        {
            // TODO: Make a discriminated union like return type which can return more useful information.

            try
            {
                var sourceStream = new FileStream(
                    FileBasedGuildRepository.GetFilePath(guildId),
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true);

                var streamReader = new StreamReader(sourceStream);
                var jsonReader = new JsonTextReader(streamReader);
                var guild = FileBasedGuildRepository.FromJToken(guildId, await JObject.ReadFromAsync(jsonReader));

                return new BorrowedGuild(
                    guild: guild,
                    data: sourceStream,
                    onReturn: FileBasedGuildRepository.ReturnGuild);
            }
            catch (FileNotFoundException)
            {
                return null;
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
        /// Returns a Guild from a JToken.
        /// </summary>
        /// <param name="guildId">An id representing the guild.</param>
        /// <param name="data">The JToken being read in.</param>
        /// <returns>A guild from the data in the JToken.</returns>
        private static Guild FromJToken(Snowflake guildId, JToken data)
        {
            JObject dataObj = data.ToObject<JObject>();
            var retVal = new Guild(guildId);

            if (dataObj.TryGetValue(SerializationFields.AllowedCreatorsRole, out JToken allowedCreatorsRoleToken))
            {
                retVal.AllowedCreatorsRole = new Snowflake(allowedCreatorsRoleToken.ToObject<UInt64>());
            }

            return retVal;
        }

        /// <summary>
        /// Returns a JToken representation of a given guild.
        /// </summary>
        /// <param name="guild">Any guild. May not be null.</param>
        /// <returns>A JToken representation of a given guild.</returns>
        private static JToken ToJToken(Guild guild)
        {
            JObject retVal = new JObject();

            if (guild.AllowedCreatorsRole.HasValue)
            {
                retVal.Add(
                    SerializationFields.AllowedCreatorsRole,
                    new JValue(guild.AllowedCreatorsRole.GetValueOrDefault().Value));
            }

            return retVal;
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
            sourceStream.Seek(0, SeekOrigin.Begin);
            sourceStream.SetLength(0);

            Formatting formatting;
            #if DEBUG
            formatting = Formatting.Indented;
            #else
            formatting = Formatting.None;
            #endif

            // TODO: In theory, we shouldn't need to copy to a string, and then serialize to file.
            //   Figure out how to serialize directly to file.
            var content = JsonConvert.SerializeObject(
                FileBasedGuildRepository.ToJToken(guild),
                formatting);

            var writer = new StreamWriter(sourceStream);
            writer.Write(content);
            writer.Flush();

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

        /// <summary>
        /// Collection of field names.
        /// </summary>
        private static class SerializationFields
        {
            // Implementer's note: no need to document fields.

            public const string AllowedCreatorsRole = "AllowedCreatorsRole";
        }
    }
}
