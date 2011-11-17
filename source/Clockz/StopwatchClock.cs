using System;
using System.Diagnostics;

namespace Clockz
{
    public class StopwatchClock : BaseClock
    {
        readonly DateTime Start;
        readonly Stopwatch Sw;
        readonly IClock Source;

        public StopwatchClock(IClock startSeed)
        {
            Source = startSeed;
            Sw = Stopwatch.StartNew();
            Start = startSeed.UtcNow;
        }

        public override DateTime UtcNow
        {
            get { return Start + Sw.Elapsed; }
        }

        public override string ToString()
        {
            return string.Format("Stopwatch (Started: {0}, Source:{1})", Start, Source);
        }
    }
}
