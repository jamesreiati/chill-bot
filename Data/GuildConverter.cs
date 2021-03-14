using Newtonsoft.Json.Linq;
using Reiati.ChillBot.Tools;
using System;
using System.IO;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// Class with helper methods for converting to/from <see cref="Guild"/> objects.
    /// </summary>
    public static class GuildConverter
    {
        /// <summary>
        /// Returns a Guild from a JToken.
        /// </summary>
        /// <param name="guildId">An id representing the guild.</param>
        /// <param name="data">The JToken being read in.</param>
        /// <returns>A guild from the data in the JToken.</returns>
        public static Guild FromJToken(Snowflake guildId, JToken data)
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

            if (dataObj.TryGetValue(SerializationFields.AnnouncementChannel, out JToken announcementChannel))
            {
                if (announcementChannel.Type != JTokenType.Integer)
                {
                    throw new InvalidDataException(
                        $"{SerializationFields.AnnouncementChannel} is expected to be an integer type.");
                }

                var announcementChannelId = new Snowflake(announcementChannel.ToObject<UInt64>());
                retVal.AnnouncementChannel = announcementChannelId;
            }

            return retVal;
        }

        /// <summary>
        /// Returns a JToken representation of a given guild.
        /// </summary>
        /// <param name="guild">Any guild. May not be null.</param>
        /// <returns>A JToken representation of a given guild.</returns>
        public static JToken ToJToken(Guild guild)
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

            if (guild.AnnouncementChannel.HasValue)
            {
                retVal.Add(
                    SerializationFields.AnnouncementChannel,
                    new JValue(guild.AnnouncementChannel.GetValueOrDefault().Value));
            }

            return retVal;
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
            public const string AnnouncementChannel = "AnnouncementChannel";
        }
    }
}
