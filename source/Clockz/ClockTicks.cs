using System;

namespace Clockz
{
    public class ClockTicks : ITicks
    {
        public static readonly ITicks SystemClockTicks = new ClockTicks(SystemClock.Instance);

        readonly IClock Source;

        public ClockTicks(IClock source)
        {
            Source = source;
        }

        public long Frequency => TimeSpan.TicksPerSecond;
        public long Ticks => Source.UtcNow.Ticks;
    }
}