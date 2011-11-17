using System;

namespace Clockz
{
    public class CompensatedClock : BaseClock
    {
        readonly IClock Source;
        readonly long DaylyCorrectionInTicks;
        readonly long TicksStart;

        /// <summary>
        /// Creates a CompensatedClock instance.
        /// </summary>
        /// <param name="daylyCorrectionInTicks">The time span to add or substract from the current system time.</param>
        public CompensatedClock(IClock source, long daylyCorrectionInTicks)
        {
            Source = source;
            DaylyCorrectionInTicks = daylyCorrectionInTicks;
            TicksStart = source.UtcNow.Ticks;
        }

        public override DateTime UtcNow
        {
            get
            {
                long current = Source.UtcNow.Ticks;
                long passedTicks = current - TicksStart;
                long correctionFactor = DaylyCorrectionInTicks * passedTicks / TimeSpan.TicksPerDay;

                return new DateTime(current + correctionFactor, DateTimeKind.Utc);
            }
        }
    }
}
