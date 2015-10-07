using System;
using System.Runtime.InteropServices;

namespace Clockz
{
    /// <summary>
    /// Clock implementation that returns the current system time.
    /// </summary>
    public class SystemClock : BaseClock
    {
        /// <summary>
        /// Returns an instance reference of the real-time singleton implementation.
        /// </summary>
        public static readonly SystemClock Instance = new SystemClock();

        protected SystemClock() { }

        public override DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }

        public override string ToString()
        {
            return "System";
        }

        /// <remarks>http://www.pinvoke.net/default.aspx/kernel32.setsystemtime</remarks>
        public class Win32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct SYSTEMTIME
            {

                public ushort Year;
                public ushort Month;
                public ushort wDayOfWeek;
                public ushort Day;
                public ushort Hour;
                public ushort Minute;
                public ushort Second;
                public ushort Milisecond;

            }

            [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
            private static extern uint SetSystemTime(ref SYSTEMTIME st);

            public static void SetUtc(DateTime timestamp)
            {
                if (timestamp.Kind != DateTimeKind.Utc) throw new ArgumentException("Kind is not Utc", "v");

                SYSTEMTIME time = new SYSTEMTIME
                {
                    Year = (ushort)timestamp.Year,
                    Month = (ushort)timestamp.Month,
                    Day = (ushort)timestamp.Day,
                    Hour = (ushort)timestamp.Hour,
                    Minute = (ushort)timestamp.Minute,
                    Second = (ushort)timestamp.Second,
                    Milisecond = (ushort)timestamp.Millisecond,
                };

                if (0 == SetSystemTime(ref time)) throw new InvalidOperationException("Failed to set time.");
            }


            [DllImport("kernel32.dll")]
            private static extern void GetSystemTime(out SYSTEMTIME lpSystemTime);

            /// <summary>
            /// Returns the current system time. This should return the same value as <see cref="System.DateTime.UtcNow"/>.
            /// </summary>
            /// <returns></returns>
            public static DateTime GetUtc()
            {
                SYSTEMTIME time;

                GetSystemTime(out time);

                return new DateTime(
                    time.Year,
                    time.Month,
                    time.Day,
                    time.Hour,
                    time.Minute,
                    time.Second,
                    DateTimeKind.Utc
                    )
                    .AddMilliseconds(time.Milisecond);
            }
        }
    }

}
