using System;

namespace Clockz.Extensions
{
    public static class TimeSpanExtensions
    {
        const double DaysInWeek = 7d;
        const double DaysInMonth = 365d / 12d;
        const double DaysInYear = 365d;

        public static double TotalWeeks(this TimeSpan val)
        {
            return val.TotalDays / DaysInWeek;
        }

        public static double TotalMonths(this TimeSpan val)
        {
            return val.TotalDays / DaysInMonth;
        }

        public static double TotalYears(this TimeSpan val)
        {
            return val.TotalDays / DaysInYear;
        }
    }
}