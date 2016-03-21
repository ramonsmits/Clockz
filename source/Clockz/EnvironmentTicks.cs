using System;
using System.Runtime.InteropServices;

namespace Clockz
{
    public class EnvironmentTicks : ITicks
    {
        private const int EnvironmentTickFrequency = 1000;
        public static readonly ITicks Instance = new EnvironmentTicks();

        public long Frequency => EnvironmentTickFrequency;
        public long Ticks => GetTickCount();

        private static readonly Func<long> GetTickCount;

        static EnvironmentTicks()
        {
            try
            {
                Win32.GetTickCount64();
                GetTickCount = () => unchecked((long)Win32.GetTickCount64());
            }
            catch
            {
                GetTickCount = () => unchecked((uint)Environment.TickCount);
            }
        }

        static class Win32
        {
            [DllImport("kernel32")]
            public static extern ulong GetTickCount64();
        }
    }
}