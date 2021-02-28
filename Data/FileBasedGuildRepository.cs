using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// A repository of <see cref="Guild"/> objects to be checked out and checked in.
    /// </summary>
    public class FileBasedGuildRepository : IGuildRepository
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
        /// <param name="recycleResult">A preallocated result that should be returned if passed in.</param>
        /// <returns>The borrowed guild.</returns>
        public async Task<GuildCheckoutResult> Checkout(Snowflake guildId, GuildCheckoutResult recycleResult = null)
        {
            var retVal = recycleResult ?? new GuildCheckoutResult();

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

                retVal.ToSuccess(new Borrowed<Guild>(
                    isntance: guild,
                    data: sourceStream,
                    onReturn: FileBasedGuildRepository.ReturnGuild));
                return retVal;
            }
            catch (FileNotFoundException)
            {
                retVal.ToDoesNotExist();
                return retVal;
            }
            catch (IOException e)
            {
                if (e.HResult == FileBasedGuildRepository.LockedFileHRResult)
                {
                    retVal.ToLocked();
                    return retVal;
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

            if (dataObj.TryGetValue(SerializationFields.OptinCreatorsRoles, out JToken optinCreatorsRolesToken))
            {
                if (optinCreatorsRolesToken.Type != JTokenType.Array)
                {
                    throw new InvalidDataException(
                        $"{SerializationFields.OptinCreatorsRoles} is expected to be an array type.");
                }

                var optinCreatorsRolesArray = optinCreatorsRolesToken.ToObject<JArray>();
                foreach (var roleToken in optinCreatorsRolesArray)
                {
                    if (roleToken.Type != JTokenType.Integer)
                    {
                        throw new InvalidDataException(
                            $"Each member of {SerializationFields.OptinCreatorsRoles} is expected to be an integer type.");
                    }

                    var roleId = new Snowflake(roleToken.ToObject<UInt64>());
                    retVal.OptinCreatorsRoles.Add(roleId);
                }
            }

            if (dataObj.TryGetValue(SerializationFields.OptinParentCatgory, out JToken optinParentCatgory))
            {
                if (optinParentCatgory.Type != JTokenType.Integer)
                {
                    throw new InvalidDataException(
                        $"{SerializationFields.OptinCreatorsRoles} is expected to be an integer type.");
                }

                var categoryId = new Snowflake(optinParentCatgory.ToObject<UInt64>());
                retVal.OptinParentCategory = categoryId;
            }

            if (dataObj.TryGetValue(SerializationFields.WelcomeChannel, out JToken welcomeChannel))
            {
                if (welcomeChannel.Type != JTokenType.Integer)
                {
                    throw new InvalidDataException(
                        $"{SerializationFields.WelcomeChannel} is expected to be an integer type.");
                }

                var welcomeChannelId = new Snowflake(welcomeChannel.ToObject<UInt64>());
                retVal.WelcomeChannel = welcomeChannelId;
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

            if (guild.OptinCreatorsRoles.Count > 0)
            {
                var optinCreatorsRolesArray = new JArray();
                foreach (var roleId in guild.OptinCreatorsRoles)
                {
                    optinCreatorsRolesArray.Add(new JValue(roleId.Value));
                }

                retVal.Add(SerializationFields.OptinCreatorsRoles, optinCreatorsRolesArray);
            }

            if (guild.OptinParentCategory.HasValue)
            {
                retVal.Add(
                    SerializationFields.OptinParentCatgory,
                    new JValue(guild.OptinParentCategory.GetValueOrDefault().Value));
            }

            if (guild.WelcomeChannel.HasValue)
            {
                retVal.Add(
                    SerializationFields.WelcomeChannel,
                    new JValue(guild.WelcomeChannel.GetValueOrDefault().Value));
            }

            return retVal;
        }

        /// <summary>
        /// The action invoked when a <see cref="Borrowed<T>"/> is being returned.
        /// </summary>
        /// <param name="guild">The guild that's being returned. May be null if the guild borrowed is null.</param>
        /// <param name="data">
        /// The object placed into the data field at the construction of the <see cref="Borrowed<T>"/>.
        /// </param>
        /// <param name="commit">
        /// True if the user wanted to commit the changes, false if they wanted them discarded.
        /// </param>
        private static void ReturnGuild(Guild guild, object data, bool commit)
        {
            var sourceStream = (FileStream)data;
            if (commit)
            {
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
            }

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

            public const string OptinCreatorsRoles = "OptinCreatorsRoles";
            public const string OptinParentCatgory = "OptinParentCatgory";
            public const string WelcomeChannel = "WelcomeChannel";
        }
    }
}
