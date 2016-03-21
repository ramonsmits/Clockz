using System.Diagnostics;

namespace Clockz
{
    public class StopwatchTicks : ITicks
    {
        public static readonly ITicks Instance = new StopwatchTicks();

        public long Frequency => Stopwatch.Frequency;
        public long Ticks => Stopwatch.GetTimestamp();
    }
}