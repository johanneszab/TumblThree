namespace Guava.RateLimiter
{
    public static class LongMath
    {
        /// <summary>
        /// Returns the sum of <param name="a"></param>
        /// and <param name="b"></param>
        /// unless it would overflow or underflow in which case
        /// <code>long.MaxValue</code> or <code>long.MaxValue</code> is returned, respectively.
        ///
        /// since 20.0
        /// </summary>
        public static long SaturatedAdd(long a, long b)
        {
            var naiveSum = unchecked(a + b);
            if ((a ^ b) < 0 | (a ^ naiveSum) >= 0)
            {
                // If a and b have different signs or a has the same sign as the result then there was no
                // overflow, return.
                return naiveSum;
            }
            // we did over/under flow, if the sign is negative we should return MAX otherwise MIN
            return naiveSum < 0 ? long.MaxValue : long.MinValue;
        }
    }
}
