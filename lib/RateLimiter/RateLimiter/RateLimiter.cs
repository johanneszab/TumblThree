using System;

namespace Guava.RateLimiter
{
    /// <summary>
    /// A rate limiter. Distributes permits at a configurable rate with each 
    /// <code>acquire</code> blocks if necessary until a permit is available
    /// </summary>
    /// <remarks>
    /// A rate limiter. Conceptually, a rate limiter distributes permits at a
    /// configurable rate. Each <code>acquire</code> blocks if necessary until a permit is
    /// available, and then takes it. Once acquired, permits need not be released.
    ///
    /// Rate limiters are often used to restrict the rate at which some
    /// physical or logical resource is accessed. This is in contrast to <code>Semaphore</code>
    /// which restricts the number of concurrent accesses instead of the rate
    /// (note though that concurrency and rate are closely related,
    /// e.g. <see href="http://en.wikipedia.org/wiki/Little's_law">Little's Law</see>).
    ///
    /// A <code>RateLimiter</code> is defined primarily by the rate at which permits
    /// are issued. Absent additional configuration, permits will be distributed at a
    /// fixed rate, defined in terms of permits per second. Permits will be distributed
    /// smoothly, with the delay between individual permits being adjusted to ensure
    /// that the configured rate is maintained.
    ///
    /// It is possible to configure a <code>RateLimiter</code> to have a warmup period 
    /// during which time the permits issued each second steadily increases until it hits the stable rate.
    ///
    /// As an example, imagine that we have a list of tasks to execute, but we don't want to
    /// submit more than 2 per second
    ///
    /// As another example, imagine that we produce a stream of data, and we want to cap it
    /// at 5kb per second. This could be accomplished by requiring a permit per byte, and specifying
    ///  a rate of 5000 permits per second:
    /// <code>  {
    ///     readonly RateLimiter rateLimiter = RateLimiter.Create(5000.0); // rate = 5000 permits per second
    ///     void SubmitPacket(byte[] packet) {
    ///         rateLimiter.Acquire(packet.Length);
    ///         networkService.Send(packet);
    ///     }
    /// </code>
    ///
    /// It is important to note that the number of permits requested _never_
    /// affect the throttling of the request itself (an invocation to <code>acquire(1)</code>
    /// and an invocation to <code>acquire(1000)</code> will result in exactly the same throttling, if any),
    /// but it affects the throttling of the _next_ request. I.e., if an expensive task
    /// arrives at an idle RateLimiter, it will be granted immediately, but it is the _next_
    /// request that will experience extra throttling, thus paying for the cost of the expensive
    /// task.
    ///
    /// Note: <code>RateLimiter</code> does not provide fairness guarantees.
    /// Author Dimitris Andreou
    /// 
    /// <see href="https://github.com/google/guava/commits/master/guava/src/com/google/common/util/concurrent/RateLimiter.java"/>
    /// git hash: 217659fcd585d0512f77c020835f2a56db059ce5
    /// </remarks>
    public abstract class RateLimiter
    {
        /// <summary>
        /// Creates a <code>RateLimiter</code> with the specified stable throughput, given as "permits per second"
        /// </summary>
        /// <remarks>
        /// Creates a <code>RateLimiter</code> with the specified stable throughput, given as
        /// "permits per second" (commonly referred to as QPS, queries per second).
        /// 
        /// The returned <code>RateLimiter</code> ensures that on average no more than <code>permitsPerSecond</code>
        /// are issued during any given second, with sustained requests
        /// being smoothly spread over each second. When the incoming request rate exceeds
        /// <code>permitsPerSecond</code> the rate limiter will release one permit every
        /// <code>(1.0 / permitsPerSecond)</code> seconds. When the rate limiter is unused,
        /// bursts of up to <code>permitsPerSecond</code> permits will be allowed, with subsequent
        /// requests being smoothly limited at the stable rate of <code>permitsPerSecond</code>.
        /// </remarks>
        /// <param name="permitsPerSecond">
        /// The rate of the returned <code>RateLimiter</code>, measured in how many permits become available per second
        /// </param>
        public static RateLimiter Create(double permitsPerSecond)
        {
            /*  The default RateLimiter configuration can save the unused permits of up to one second.
                This is to avoid unnecessary stalls in situations like this: A RateLimiter of 1qps,
                and 4 threads, all calling acquire() at these moments:

                T0 at 0 seconds
                T1 at 1.05 seconds
                T2 at 2 seconds
                T3 at 3 seconds
            
                Due to the slight delay of T1, T2 would have to sleep till 2.05 seconds,
                and T3 would also have to sleep till 3.05 seconds. */

            return Create(SleepingStopwatch.CreateFromSystemTimer(), permitsPerSecond);
        }

        internal static RateLimiter Create(ISleepingStopwatch stopwatch, double permitsPerSecond)
        {
            SmoothRateLimiter.SmoothBursty rateLimiter = new SmoothRateLimiter.SmoothBursty(stopwatch, 1.0 /* maxBurstSeconds */);
            rateLimiter.SetRate(permitsPerSecond);
            return rateLimiter;
        }

        /// <summary>
        /// <para>
        /// The returned <code>RateLimiter</code> is intended for cases where the resource that actually
        /// fulfills the requests (e.g., a remote server) needs "warmup" time, rather than
        /// being immediately accessed at the stable (maximum) rate.
        /// </para>
        /// <para>
        /// The returned <code>RateLimiter</code> starts in a "cold" state (i.e. the warmup period
        /// will follow), and if it is left unused for long enough, it will return to that state.
        /// </para>
        /// </summary>
        /// <param name="permitsPerSecond">The rate of the returned <code>RateLimiter</code>, measured in how many permits become available per second</param>
        /// <param name="warmupPeriod">The duration of the period where the <code>RateLimiter</code> ramps up its rate, before reaching its stable (maximum) rate</param>
        /// <param name="unit">The time unit of the warmupPeriod argument</param>
        /// <param name="coldFactor"></param>
        public static RateLimiter Create(double permitsPerSecond, long warmupPeriod, TimeUnit unit = TimeUnit.Seconds, double coldFactor = 3.0)
        {
            if(warmupPeriod < 0)
                throw new ArgumentOutOfRangeException(nameof(warmupPeriod), "WarmupPeriod must not be negative");

            return Create(SleepingStopwatch.CreateFromSystemTimer(), permitsPerSecond, warmupPeriod, unit, coldFactor);
        }

        internal static RateLimiter Create(ISleepingStopwatch stopwatch, double permitsPerSecond, long warmupPeriod, TimeUnit unit, double coldFactor)
        {
            RateLimiter rateLimiter = new SmoothRateLimiter.SmoothWarmingUp(stopwatch, warmupPeriod, unit, coldFactor);
            rateLimiter.SetRate(permitsPerSecond);
            return rateLimiter;
        }

        /// <summary>
        /// The underlying timer; used both to measure elapsed time and sleep as necessary. A separate object to facilitate testing.
        /// </summary>
        private readonly ISleepingStopwatch _stopwatch;

        private volatile object _mutexDoNotUseDirectly;

        private object Mutex()
        {
            object mutex = _mutexDoNotUseDirectly;
            if (mutex == null)
            {
                lock (this)
                {
                    mutex = _mutexDoNotUseDirectly;
                    if (mutex == null)
                    {
                        _mutexDoNotUseDirectly = mutex = new object();
                    }
                }
            }
            return mutex;
        }

        protected RateLimiter(ISleepingStopwatch stopwatch)
        {
            if(stopwatch == null)
                throw new ArgumentNullException(nameof(stopwatch));

            this._stopwatch = stopwatch;
        }

        /// <summary>Updates the stable rate of this <code>RateLimiter</code></summary>
        /// <remarks>
        /// Updates the stable rate of this <code>RateLimiter</code>, that is, the
        /// <code>permitsPerSecond</code> argument provided in the factory method that
        /// constructed the <code>RateLimiter</code>. Currently throttled threads will *not*
        /// be awakened as a result of this invocation, thus they do not observe the new rate;
        /// only subsequent requests will.
        /// <para>
        /// Note though that, since each request repays (by waiting, if necessary) the cost
        /// of the <i>previous</i> request, this means that the very next request
        /// after an invocation to <code>setRate</code> will not be affected by the new rate;
        /// it will pay the cost of the previous request, which is in terms of the previous rate.
        /// </para>
        /// <para>
        /// The behavior of the <code>RateLimiter</code> is not modified in any other way,
        /// e.g. if the <code>RateLimiter</code> was configured with a warmup period of 20 seconds,
        /// it still has a warmup period of 20 seconds after this method invocation.
        /// </para>
        /// </remarks>
        /// <param name="permitsPerSecond">The new stable rate of this <code>RateLimiter</code></param>
        public void SetRate(double permitsPerSecond)
        {
            if (permitsPerSecond <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(permitsPerSecond), "Rate must be positive");

            lock (Mutex())
                DoSetRate(permitsPerSecond, _stopwatch.ReadMicros());
        }

        protected abstract void DoSetRate(double permitsPerSecond, long nowMicros);

        /// <summary>Returns stable rate (permits per second)</summary>
        /// <remarks>
        /// Returns the stable rate (as <code>permitsPerSecond</code>) with which this
        /// <code>RateLimiter</code> is configured with. The initial value of this is the same as
        /// the <code>permitsPerSecond</code> argument passed in the factory method that produced
        /// this <code>RateLimiter</code>, and it is only updated after invocations
        /// to <code>setRate</code>.
        /// </remarks>
        public double GetRate()
        {
            lock (Mutex())
                return DoGetRate();
        }

        protected abstract double DoGetRate();

        /// <summary>
        /// Acquires the given number of permits from this <code>RateLimiter</code>, blocking until the
        /// request can be granted. Tells the amount of time slept, if any.
        /// </summary>
        /// <param name="permits">The number of permits to acquire</param>
        /// <returns>Time spent sleeping to enforce rate, in seconds; 0.0 if not rate-limited</returns>
        public double Acquire(int permits = 1)
        {
            long microsToWait = Reserve(permits);
            _stopwatch.SleepMicrosUninterruptibly(microsToWait);
            return 1.0 * microsToWait / TimeUnit.Seconds.ToMicros(1L);
        }

        /// <summary>
        /// Reserves the given number of permits from this <code>RateLimiter</code> for future use, returning
        /// the number of microseconds until the reservation can be consumed.
        /// </summary>
        /// <returns>Time in microseconds to wait until the resource can be acquired, never negative</returns>
        private long Reserve(int permits)
        {
            CheckPermits(permits);
            lock (Mutex())
            {
                return ReserveAndGetWaitLength(permits, _stopwatch.ReadMicros());
            }
        }

        /// <summary>
        /// Acquires the given number of permits from this <code>RateLimiter</code> if it can be obtained
        /// without exceeding the specified <code>timeout</code>, or returns <code>false</code>
        /// immediately (without waiting) if the permits would not have been granted
        /// before the timeout expired.
        /// </summary>
        /// <param name="permits">The number of permits to acquire</param>
        /// <param name="timeout">The maximum time to wait for the permits. Negative values are treated as zero</param>
        /// <param name="unit">The time unit of the timeout argument</param>
        /// <returns><code>true</code> if the permits were acquired, <code>false</code> otherwise</returns>
        public bool TryAcquire(int permits = 1, long timeout = 0, TimeUnit unit = TimeUnit.Microseconds)
        {
            long timeoutMicros = Math.Max(unit.ToMicros(timeout), 0);
            CheckPermits(permits);
            long microsToWait;
            lock (Mutex())
            {
                long nowMicros = _stopwatch.ReadMicros();
                if (!CanAcquire(nowMicros, timeoutMicros))
                {
                    return false;
                }
                else
                {
                    microsToWait = ReserveAndGetWaitLength(permits, nowMicros);
                }
            }
            _stopwatch.SleepMicrosUninterruptibly(microsToWait);
            return true;
        }

        private bool CanAcquire(long nowMicros, long timeoutMicros)
        {
            return QueryEarliestAvailable(nowMicros) - timeoutMicros <= nowMicros;
        }

        /// <summary>
        /// Reserves next ticket and returns the wait time that the caller must wait for.
        /// </summary>
        /// <returns>Required wait time, never negative</returns>
        private long ReserveAndGetWaitLength(int permits, long nowMicros)
        {
            long momentAvailable = ReserveEarliestAvailable(permits, nowMicros);
            return Math.Max(momentAvailable - nowMicros, 0);
        }

        /// <summary>
        /// Returns the earliest time that permits are available (with one caveat)
        /// </summary>
        /// <param name="nowMicros"></param>
        /// <returns>The time that permits are available, or, if permits are available immediately, an arbitrary past or present time</returns>
        protected abstract long QueryEarliestAvailable(long nowMicros);

        /// <summary>
        /// Reserves the requested number of permits and returns the time that those permits can be used (with one caveat)
        /// </summary>
        /// <returns>The time that the permits may be used, or, if the permits may be used immediately, an arbitrary past or present time</returns>
        protected abstract long ReserveEarliestAvailable(int permits, long nowMicros);

        public override string ToString()
        {
            return $"RateLimiter[stableRate={GetRate()}fqps]";
        }

        private static void CheckPermits(int permits)
        {
            if(permits <= 0)
                throw new ArgumentOutOfRangeException(nameof(permits), $"Requested permits ({permits}) must be positive");
        }
    }
}