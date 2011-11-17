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

            public static void Set(DateTime v)
            {
                if (v.Kind != DateTimeKind.Utc) throw new ArgumentException("Kind is not Utc", "v");

                SYSTEMTIME time = new SYSTEMTIME
                {
                    Year = (ushort)v.Year,
                    Month = (ushort)v.Month,
                    Day = (ushort)v.Day,
                    Hour = (ushort)v.Hour,
                    Minute = (ushort)v.Minute,
                    Second = (ushort)v.Second,
                    Milisecond = (ushort)v.Millisecond,
                };

                if (0 == SetSystemTime(ref time)) throw new InvalidOperationException("Failed to set time.");
            }
        }
    }

}
