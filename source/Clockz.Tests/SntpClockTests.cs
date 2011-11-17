using NUnit.Framework;

namespace Clockz.Tests
{
    [TestFixture]
    public class SntpClockTests
    {
        [Test, Explicit]
        public void SetSystemTime()
        {
            var sntpClock = new SntpClock("pool.ntp.org");
            var now = sntpClock.UtcNow;
            SystemClock.Win32.Set(now);
        }
    }
}
