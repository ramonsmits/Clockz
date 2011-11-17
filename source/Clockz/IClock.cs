using System;

namespace Clockz
{
    /// <summary>
    /// Clock interface
    /// </summary>
    public interface IClock
    {
        /// <summary>
        /// Retrieve current date and time.
        /// </summary>
        /// <returns></returns>
        DateTime UtcNow { get; }

        /// <summary>
        /// Retrieve todays date.
        /// </summary>
        DateTime Today { get; }
    }
}
