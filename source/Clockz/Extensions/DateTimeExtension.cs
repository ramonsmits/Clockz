using System;

namespace Clockz.Extensions
{
    /// <summary>
    /// This extension class helps to easily convert to/from unix timestamps.
    /// </summary>
    public static class DateTimeExtension
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Convert the passed datetime (in utc) to a unixtimestamp that is commonly used in java.
        /// </summary>
        /// <param name="utc">A <see cref="DateTime"/> value where Kind is UTC.</param>
        /// <returns>Seconds since unix timestamp epoch.</returns>
        /// <exception cref="ArgumentException">Thrown when 'utc' argument is not of kind utc.</exception>
        public static long ToEpochSeconds(this DateTime utc)
        {
            if (utc.Kind != DateTimeKind.Utc) throw new ArgumentException("The passed DateTime kind is NOT utc.", "utc");
            return (long)((utc - Epoch).TotalSeconds);
        }

        /// <summary>
        /// Convert the passed unix timestamp to a UTC <see cref="DateTime"/> value that is commonly used in java.
        /// </summary>
        /// <param name="secondsSindsEpoch">Total seconds passed since unix timestamp epoch.</param>
        /// <returns>A <see cref="DateTime"/> where its Kind is UTC. </returns>
        public static DateTime FromEpochSeconds(this long secondsSindsEpoch)
        {
            return Epoch.AddSeconds(secondsSindsEpoch);
        }

        /// <summary>
        /// Convert the passed datetime (in utc) to a unixtimestamp based on milliseconds commonly used in javascript.
        /// </summary>
        /// <param name="utc">A <see cref="DateTime"/> value where Kind is UTC.</param>
        /// <returns>Seconds since unix timestamp epoch.</returns>
        /// <exception cref="ArgumentException">Thrown when 'utc' argument is not of kind utc.</exception>
        public static long ToEpochMilliseconds(this DateTime utc)
        {
            if (utc.Kind != DateTimeKind.Utc) throw new ArgumentException("The passed DateTime kind is NOT utc.", "utc");
            return (long)((utc - Epoch).TotalMilliseconds);
        }

        /// <summary>
        /// Convert the passed unix millisecond timestamp to a UTC <see cref="DateTime"/> value that is commonly used in javascript.
        /// </summary>
        /// <param name="secondsSindsEpoch">Total seconds passed since unix timestamp epoch.</param>
        /// <returns>A <see cref="DateTime"/> where its Kind is UTC. </returns>
        public static DateTime FromEpochMilliseconds(this long secondsSindsEpoch)
        {
            return Epoch.AddMilliseconds(secondsSindsEpoch);
        }

        /// <summary>
        /// Rounds a datetime based the interval specified.
        /// </summary>
        public static DateTime Round(this DateTime dt, TimeSpan d)
        {
            return new DateTime(((dt.Ticks + d.Ticks - 1) / d.Ticks) * d.Ticks);
        }
    }
}
