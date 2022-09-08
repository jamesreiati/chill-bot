namespace Reiati.ChillBot.Tools
{
    /// <summary>
    /// Extension methods related to strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Truncates a string to a maximum length.
        /// </summary>
        /// <param name="originalString">The string to consider truncating.</param>
        /// <param name="maxLength">The maximum length of the string.</param>
        /// <param name="continuationString">A string to append at the end of the truncated string.</param>
        /// <returns>The truncated string.</returns>
        public static string Truncate(this string originalString, int maxLength, string continuationString = "...")
        {
            if (string.IsNullOrEmpty(originalString) || originalString.Length <= maxLength)
            {
                return originalString;
            }

            int lengthToTakeFromOriginal = maxLength;
            if (!string.IsNullOrEmpty(continuationString))
            {
                lengthToTakeFromOriginal -= continuationString.Length;
                return originalString.Substring(0, lengthToTakeFromOriginal) + continuationString;
            }
            else
            {
                return originalString.Substring(0, lengthToTakeFromOriginal);
            }
        }
    }
}
