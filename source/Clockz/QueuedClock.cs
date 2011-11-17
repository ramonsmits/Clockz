using System;
using System.Collections.Generic;

namespace Clockz
{
    /// <summary>
    /// Clock implementation that returns specific values based on a queue.
    /// </summary>
    public class QueuedClock : IClock
    {
        readonly Queue<DateTime> Values;

        /// <summary>
        /// Creates a QueuedClock instance.
        /// </summary>
        /// <param name="values">The values to return when IClock.UtcNow is called in FIFO order.</param>
        public QueuedClock(IEnumerable<DateTime> values)
        {
            Values = new Queue<DateTime>(values);
        }

        public virtual DateTime UtcNow
        {
            get { return Values.Dequeue(); }
        }

        public virtual DateTime Today
        {
            get { return Values.Dequeue().Date; }
        }
    }
}
