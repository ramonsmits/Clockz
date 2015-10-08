using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace TimeInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            var interval = 10000;//TimeSpan.Parse(args[1]);

            var system = Clockz.SystemClock.Instance;
            var stopwatch = new Clockz.StopwatchClock(system);
            stopwatch.UtcNow.AddDays(0);
            stopwatch = new Clockz.StopwatchClock(system);
            var ntp = new Clockz.SntpClock();
            ntp.UtcNow.AddDays(0);
            ntp = new Clockz.SntpClock();

            Console.Title = "TimeInfo";
            while (true)
            {
                try
                {
                    var systemTime = system.UtcNow;
                    var stopwatchTime = stopwatch.UtcNow;
                    var ntpTime = ntp.UtcNow;

                    var drift = systemTime - stopwatchTime;
                    var ntpDiff = ntpTime - systemTime;
                    var ntpStopwatchDiff = stopwatchTime - ntpTime;

                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                    Console.WriteLine(
                        "System: {0:T} NTP: {1:T} Stopwatch: {2:T}  System/Stopwatch: {3,7:N}ms  NTP/System: {4,7:N}ms  Stopwatch/NTP: {5,7:N}ms",
                        systemTime,
                        ntpTime,
                        stopwatchTime,
                        drift.TotalMilliseconds,
                        ntpDiff.TotalMilliseconds,
                        ntpStopwatchDiff.TotalMilliseconds
                        );

                }
                finally
                {
                    Thread.Sleep(interval);
                }
            }
        }
    }
}
