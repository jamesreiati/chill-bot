using System;
using Reiati.ChillBot.Tools;

namespace Reiati.ChillBot.Data
{
    /// <summary>
    /// Data to run the bot in a guild.
    /// </summary>
    public class Guild
    {
        /// <summary>
        /// Constructs a new <see cref="Guild"/>.
        /// </summary>
        /// <param name="id"></param>
        public Guild(Snowflake id)
        {
            this.Id = id;
        }

        /// <summary>
        /// The id representing this guild.
        /// </summary>
        public Snowflake Id { get; }

        /// <summary>
        /// The id representing the role allowed to create channels.
        /// </summary>
        /// <value>The id representing the role, or null if all users are allowed to create channels.</value>
        public Snowflake? AllowedCreatorsRole { get; set; }
    }
}
