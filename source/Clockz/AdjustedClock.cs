using System;

namespace Clockz
{
    public class AdjustedClock : BaseClock
    {
        readonly IClock Source;
        readonly TimeSpan Offset;

        public AdjustedClock(IClock source, TimeSpan offset)
        {
            Source = source;
            Offset = offset;
        }

        public AdjustedClock(IClock source, IClock other)
        {
            Source = source;
            Offset = other.UtcNow - source.UtcNow;
        }

        public override DateTime UtcNow
        {
            get { return Source.UtcNow + Offset; }
        }

        public override string ToString()
        {
            return string.Format("Adjusted (Offset: {0}, Source: {1})", Offset, Source);
        }
    }
}
