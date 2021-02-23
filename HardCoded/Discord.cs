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
    }
}