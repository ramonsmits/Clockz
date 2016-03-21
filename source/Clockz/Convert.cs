using System;

namespace Clockz
{
    public class TicksConvert
    {
        /// <summary>
        /// Convert the amount of ticks between tick frequencies. Due to the usage of longs it is imported in which order to do the multiplications and divides.
        /// </summary>
        public static long ToTickFrequency(long sourceTicks, long sourceFrequency, long destinationFrequency)
        {
            return destinationFrequency * sourceTicks / sourceFrequency;
        }

        /// <summary>
        /// Converts an amount of ticks to a TimeSpan.
        /// </summary>
        public static TimeSpan ToTimeSpan(long sourceTicks, long sourceFrequency)
        {
            return TimeSpan.FromSeconds(sourceTicks / (double)sourceFrequency);
        }
    }
}