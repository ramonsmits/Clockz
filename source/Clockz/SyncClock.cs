using System;

namespace Clockz
{
    public class SyncClock : BaseClock
    {
        readonly IClock Source;
        readonly IClock Other;
        readonly TimeSpan Duration;
        AdjustedClock _adjusted;
        DateTime _sync;

        public SyncClock(IClock source, IClock other, TimeSpan duration)
        {
            Source = source;
            Other = other;
            Duration = duration;
            _adjusted = new AdjustedClock(Other, source);
        }

        public override DateTime UtcNow
        {
            get
            {
                var now = _adjusted.UtcNow;

                // Check if we need to re-sync our clock because the re-sync duration is passed.
                if (_sync < now)
                {
                    now = Source.UtcNow;                                // Grab new time to sync to
                    _sync = now + Duration;                             // Calculate new expiration
                    var difference = _sync - now;                       // Calculate clock differences
                    _adjusted = new AdjustedClock(Other, difference);   // and create a new adjusted clock based on difference.
                }

                return now;
            }
        }
    }
}
