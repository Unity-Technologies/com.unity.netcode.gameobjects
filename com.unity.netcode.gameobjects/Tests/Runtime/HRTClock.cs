using System;
using System.Diagnostics;
using System.Threading;

namespace MLAPI.RuntimeTests
{
    public sealed class HRTClock : IDisposable
    {
        private readonly long m_MaxIdleTime = TimeSpan.FromSeconds(10).Ticks;
        private const long k_TicksMultiplier = 1000 * TimeSpan.TicksPerMillisecond;

        private readonly ThreadLocal<DateTime> m_StartTime =
           new ThreadLocal<DateTime>(() => DateTime.UtcNow, false);

        private readonly ThreadLocal<double> m_StartTimestamp =
           new ThreadLocal<double>(() => Stopwatch.GetTimestamp(), false);

        public DateTime UtcNow
        {
            get
            {
                double endTimestamp = Stopwatch.GetTimestamp();

                var durationInTicks = (endTimestamp - m_StartTimestamp.Value) / Stopwatch.Frequency * k_TicksMultiplier;
                if (durationInTicks >= m_MaxIdleTime)
                {
                    m_StartTimestamp.Value = Stopwatch.GetTimestamp();
                    m_StartTime.Value = DateTime.UtcNow;
                    return m_StartTime.Value;
                }
                return m_StartTime.Value.AddTicks((long)durationInTicks);
            }
        }

        public void Dispose()
        {
            m_StartTime.Dispose();
            m_StartTimestamp.Dispose();
        }
    }
}
