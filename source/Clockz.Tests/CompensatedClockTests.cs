using System;
using NUnit.Framework;

namespace Clockz.Tests
{
    [TestFixture]
    public class CompensatedClockTests
    {
        [Test]
        public void Test()
        {
            const long correction = 100;

            long now = DateTime.UtcNow.Ticks;

            var f = new FixedClock(new DateTime(now,DateTimeKind.Utc));

            var c = new CompensatedClock(f, correction);

            Assert.AreEqual(c.UtcNow, f.FixedValue);

            f.FixedValue = new DateTime(now + TimeSpan.TicksPerDay, DateTimeKind.Utc);

            Assert.AreEqual(c.UtcNow, new DateTime(now + TimeSpan.TicksPerDay + correction));

            f.FixedValue = new DateTime(now + TimeSpan.TicksPerDay + TimeSpan.TicksPerDay, DateTimeKind.Utc);

            Assert.AreEqual(c.UtcNow, new DateTime(now + TimeSpan.TicksPerDay + TimeSpan.TicksPerDay + correction + correction));
        }
    }
}
