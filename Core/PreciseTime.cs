using System;
using System.Diagnostics;

namespace Cloudy
{
    /// <summary>
    /// Provides a high-precision (down to a tenths of microseconds) 
    /// <see cref="DateTime"/> value for <see cref="UtcNow"/>.
    /// </summary>
    public static class PreciseTime
    {
        static readonly long startTimestamp = Stopwatch.GetTimestamp();
        // We just preserve milliseconds precision from DateTimeOffset, which is precise enough for 
        // adding the timestamps on top.
        static readonly long startTicks = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond;

        /// <summary>
        /// Gets the high-precision value of the current UTC date time, with 10.000.000ths of a 
        /// second precision (within the current process).
        /// </summary>
        public static DateTime UtcNow => new DateTime(GetUtcNowTicks(), DateTimeKind.Utc);

        static long GetUtcNowTicks()
        {
            // Calculate the fractional elapsed seconds since we started
            double elapsedTicks = (Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency;
            // Discard milliseconds, which we're getting from DateTime.UtcNow ticks in startTicks field
            double microsecTicks = (elapsedTicks * 1000) - (int)elapsedTicks * 1000;

            return startTicks + (long)(microsecTicks * 10000);
        }
    }
}
