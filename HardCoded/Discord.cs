using System;
using System.IO;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.HardCoded
{
    /// <summary>
    /// Hard coded values related to Discord.
    /// </summary>
    public static class Discord
    {
        /// <summary>
        /// The path to the token file.
        /// </summary>
        public static readonly string TokenFilePath = Path.Combine(".", "discordtoken.txt");

        /// <summary>
        /// The id of the allowed creators.
        /// </summary>
        public static readonly Snowflake AllowedCreatorsRole = new Snowflake(802698811834105869);

        /// <summary>
        /// The id of the opt-ins category.
        /// </summary>
        /// <remarks>
        /// The bot must have access to this category.
        /// Everybody else must not have access to this category.
        /// </remarks>
        public static readonly Snowflake OptInsCategory = new Snowflake(812896964131684352);

        /// <summary>
        /// The id of the invite only category.
        /// </summary>
        /// <remarks>
        /// The bot must have access to this category.
        /// Everybody else must not have access to this category.
        /// </remarks>
        public static readonly Snowflake InviteOnlyCategory = new Snowflake(812899587403546635);
    }
}