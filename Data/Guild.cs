using System;
using System.Collections.Generic;
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
        /// The ids representing the roles allowed to create opt-in channels.
        /// </summary>
        /// <value>Never null.</value>
        public ICollection<Snowflake> OptinCreatorsRoles { get; } = new List<Snowflake>();

        /// <summary>
        /// The ids representing the roles allowed to update opt-in channels.
        /// </summary>
        /// <value>Never null.</value>
        public ICollection<Snowflake> OptinUpdatersRoles { get; } = new List<Snowflake>();

        /// <summary>
        /// The id representing the channel category under which all opt-in channels are to be created.
        /// </summary>
        /// <value>Null if opt-ins are disabled on this server.</value>
        public Snowflake? OptinParentCategory { get; set; }

        /// <summary>
        /// The id representing the channel under which welcome messages are to be sent.
        /// </summary>
        /// <value>Null if welcome messages are not to be sent.</value>
        public Snowflake? WelcomeChannel { get; set; }

        /// <summary>
        /// The id representing the channel under which announcement messages are to be sent.
        /// </summary>
        /// <value>Null if announcement messages are not to be sent.</value>
        public Snowflake? AnnouncementChannel { get; set; }
    }
}
