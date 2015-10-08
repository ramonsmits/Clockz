using System;
using NUnit.Framework;

namespace Clockz.Tests
{
    [TestFixture, Timeout(5000)]
    public class SntpClockTests
    {
        private const string NtpServer = "0.pool.ntp.org";

        [Test, Explicit]
        public void SetSystemTime()
        {
            var sntpClock = new SntpClock();
            var now = sntpClock.UtcNow;
            SystemClock.Win32.SetUtc(now);
        }

        [Test]
        public void GetUtcNowDefault()
        {
            var clock = new SntpClock();
            var difference = DateTime.UtcNow - clock.UtcNow;
            Assert.AreEqual(0, difference.TotalMilliseconds, 1000 * 60, "System Utc clock difference with NTP clock is too large: " + difference);
        }

        [Test]
        public void GetUtcNowCustom()
        {
            var clock = new SntpClock(NtpServer);
            var difference = DateTime.UtcNow - clock.UtcNow;
            Assert.AreEqual(0, difference.TotalMilliseconds, 1000 * 60, "System Utc clock difference with NTP clock is too large: " + difference);
        }
    }
}
