using System;
using System.Diagnostics;

namespace MultiplayerExample.Utilities
{
    public static class TimeExt
    {
        private static readonly double OSTimeStampToTimeSpanTicksMultiplier = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        static TimeExt()
        {
            if (!Stopwatch.IsHighResolution)
            {
                throw new Exception("Your machine does not support running this game because it does not support a high resolution timer.");
            }
        }

        public static long GetOSTimestamp() => Stopwatch.GetTimestamp();

        public static TimeSpan ConvertToTimeSpan(long fromOSTimeStamp, long toOSTimeStamp)
        {
            var osTimeStampDiff = toOSTimeStamp - fromOSTimeStamp;
            return ConvertToTimeSpan(osTimeStampDiff);
        }

        public static TimeSpan ConvertToTimeSpan(long osTimeStampDiff)
        {
            var timeInTicksDouble = osTimeStampDiff * OSTimeStampToTimeSpanTicksMultiplier;
            var timeInTicks = (long)timeInTicksDouble;
            return new TimeSpan(timeInTicks);
        }
    }
}
