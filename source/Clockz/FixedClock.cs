using System;

namespace Clockz
{
    /// <summary>
    /// Clock implementation that always returns the same time.
    /// </summary>
    public class FixedClock : BaseClock
    {
        public DateTime FixedValue { get; set; }

        /// <summary>
        /// Creates a FixedClock instance.
        /// </summary>
        /// <param name="value">The date time value to return</param>
        /// <exception cref="ArgumentException">Thrown when value.Kind is not equal to UTC.</exception>
        public FixedClock(DateTime value)
        {
            if (value.Kind != DateTimeKind.Utc) throw new ArgumentException("Kind is NOT UTC.", "value");
            FixedValue = value;
        }

        public override DateTime UtcNow
        {
            get { return FixedValue; }
        }
    }
}
