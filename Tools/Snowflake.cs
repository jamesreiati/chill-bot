using System;

namespace Reiati.ChillBot.Tools
{
    /// <summary>
    /// Discord's Identifier system.
    /// </summary>
    /// <remarks>
    /// For Discord documentation, see https://discord.com/developers/docs/reference#snowflakes.
    /// </remarks>
    public readonly struct Snowflake
    {
        /// <summary>
        /// The value of the snowflake.
        /// </summary>
        public UInt64 Value { get; }

        /// <summary>
        /// Constructs a new Snowflake.
        /// </summary>
        /// <param name="value">Any value.</param>
        public Snowflake(UInt64 value)
        {
            this.Value = value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Value.ToString();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            bool wasConverted = false;
            Snowflake other = default;

            if (obj is UInt64)
            {
                wasConverted = true;
                other = new Snowflake((UInt64)obj);
            }

            if (obj is Snowflake)
            {
                wasConverted = true;
                other = (Snowflake)obj;
            }

            return wasConverted ? other.Value == this.Value : false;
        }

        /// <summary>
        /// Allows Snowflakes to be explicitly cast to UInt64s.
        /// </summary>
        /// <param name="snowflake">Any snowflake.</param>
        public static explicit operator UInt64(Snowflake snowflake)
        {
            return snowflake.Value;
        }

        /// <summary>
        /// Allows UInt64s to be implicitly cast to Snowflakes.
        /// </summary>
        /// <param name="uint64"></param>
        public static implicit operator Snowflake(UInt64 uint64)
        {
            return new Snowflake(uint64);
        }

        /// <summary>
        /// Compares two Snowflakes by value.
        /// </summary>
        /// <param name="left">Any Snowflake.</param>
        /// <param name="right">Any Snowflake.</param>
        /// <returns>True if they are the same, false otherwise.</returns>
        public static bool operator==(Snowflake left, Snowflake right)
        {
            return left.Value == right.Value;
        }

        /// <summary>
        /// Compares two Snowflakes by value.
        /// </summary>
        /// <param name="left">Any Snowflake.</param>
        /// <param name="right">Any Snowflake.</param>
        /// <returns>True if they are the different, false otherwise.</returns>
        public static bool operator!=(Snowflake left, Snowflake right)
        {
            return left.Value != right.Value;
        }
    }
}
