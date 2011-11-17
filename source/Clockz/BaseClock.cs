using System;

namespace Clockz
{
    public abstract class BaseClock : IClock
    {
        public abstract DateTime UtcNow { get; }

        public virtual DateTime Today
        {
            get { return UtcNow.Date; }
        }
    }
}
