using System;

namespace Guava.RateLimiter
{
    public enum TimeUnit
    {
        Nanoseconds,
        Microseconds,
        Milliseconds,
        Seconds,
        Minutes,
        Hours,
        Days
    }

    public static class TimeUnitExtensions
    {
        public static double ToMicros(this TimeUnit unit, double value)
        {
            switch (unit)
            {
                case TimeUnit.Nanoseconds:
                    return value/1000;
                case TimeUnit.Microseconds:
                    return value;
                case TimeUnit.Milliseconds:
                    return value*1000;
                case TimeUnit.Seconds:
                    return value*1000000;
                case TimeUnit.Minutes:
                    return value*60000000;
                case TimeUnit.Hours:
                    return value*3600000000;
                case TimeUnit.Days:
                    return value*86400000000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        public static long ToMicros(this TimeUnit unit, long value)
        {
            switch (unit)
            {
                case TimeUnit.Nanoseconds:
                    return value/1000;
                case TimeUnit.Microseconds:
                    return value;
                case TimeUnit.Milliseconds:
                    return value*1000;
                case TimeUnit.Seconds:
                    return value*1000000;
                case TimeUnit.Minutes:
                    return value*60000000;
                case TimeUnit.Hours:
                    return value*3600000000;
                case TimeUnit.Days:
                    return value*86400000000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        public static long ToMillis(this TimeUnit unit, long value)
        {
            switch (unit)
            {
                case TimeUnit.Nanoseconds:
                    return value/1000000;
                case TimeUnit.Microseconds:
                    return value / 1000;
                case TimeUnit.Milliseconds:
                    return value;
                case TimeUnit.Seconds:
                    return value*1000;
                case TimeUnit.Minutes:
                    return value*60000;
                case TimeUnit.Hours:
                    return value*3600000;
                case TimeUnit.Days:
                    return value*86400000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        public static long ToNanos(this TimeUnit unit, long value)
        {
            switch (unit)
            {
                case TimeUnit.Nanoseconds:
                    return value;
                case TimeUnit.Microseconds:
                    return value * 1000;
                case TimeUnit.Milliseconds:
                    return value * 1000000;
                case TimeUnit.Seconds:
                    return value * 1000000000;
                case TimeUnit.Minutes:
                    return value * 60000000000;
                case TimeUnit.Hours:
                    return value * 3600000000000;
                case TimeUnit.Days:
                    return value * 86400000000000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }
    }
}