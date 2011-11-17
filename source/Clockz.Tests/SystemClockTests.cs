using System;
using NUnit.Framework;
using Microsoft.Win32;

namespace Clockz.Tests
{
    [TestFixture]
    public class SystemClockTests
    {
        [Test, Explicit]
        public void Set()
        {
            SystemEvents.TimeChanged += SystemEvents_TimeChanged;

            try
            {
                var now = DateTime.UtcNow;
                SystemClock.Win32.Set(now);
            }
            finally
            {
                SystemEvents.TimeChanged -= SystemEvents_TimeChanged;
            }
        }

        void SystemEvents_TimeChanged(object sender, EventArgs e)
        {
            Console.WriteLine("SystemEvents_TimeChanged");
        }
    }
}
