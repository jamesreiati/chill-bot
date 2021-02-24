using System;
using System.Runtime.CompilerServices;

namespace Reiati.ChillBot.Tools
{
    /// <summary>
    /// Helper class for validating the arguments of parameters.
    /// </summary>
    public static class ValidateArg
    {
        /// <summary>
        /// Throws if some value is null.
        /// </summary>
        /// <param name="value">Any value.</param>
        /// <param name="paramName">The name of the parameter to put in the exception.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull(object value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Throws if some string is null or whitespace.
        /// </summary>
        /// <param name="value">Any value.</param>
        /// <param name="paramName">The name of the parameter to put in the exception.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}
