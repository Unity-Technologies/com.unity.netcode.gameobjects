using System;

namespace Unity.Netcode
{
    internal class ConnectionRtt
    {
        private double[] m_RttSendTimes; // times at which packet were sent for RTT computations
        private int[] m_SendSequence; // tick, or other key, at which packets were sent (to allow matching)
        private double[] m_MeasuredLatencies; // measured latencies (ring buffer)
        private int m_LatenciesBegin = 0; // ring buffer begin
        private int m_LatenciesEnd = 0; // ring buffer end

        /// <summary>
        /// Round-trip-time data
        /// </summary>
        public struct Rtt
        {
            public double BestSec; // best RTT
            public double AverageSec; // average RTT
            public double WorstSec; // worst RTT
            public double LastSec; // latest ack'ed RTT
            public int SampleCount; // number of contributing samples
        }

        public ConnectionRtt()
        {
            m_RttSendTimes = new double[NetworkConfig.RttWindowSize];
            m_SendSequence = new int[NetworkConfig.RttWindowSize];
            m_MeasuredLatencies = new double[NetworkConfig.RttWindowSize];
        }

        /// <summary>
        /// Returns the Round-trip-time computation for this client
        /// </summary>
        public Rtt GetRtt()
        {
            var ret = new Rtt();
            var index = m_LatenciesBegin;
            double total = 0.0;
            ret.BestSec = m_MeasuredLatencies[m_LatenciesBegin];
            ret.WorstSec = m_MeasuredLatencies[m_LatenciesBegin];

            while (index != m_LatenciesEnd)
            {
                total += m_MeasuredLatencies[index];
                ret.SampleCount++;
                ret.BestSec = Math.Min(ret.BestSec, m_MeasuredLatencies[index]);
                ret.WorstSec = Math.Max(ret.WorstSec, m_MeasuredLatencies[index]);
                index = (index + 1) % NetworkConfig.RttAverageSamples;
            }

            if (ret.SampleCount != 0)
            {
                ret.AverageSec = total / ret.SampleCount;
                // the latest RTT is one before m_LatenciesEnd
                ret.LastSec = m_MeasuredLatencies[(m_LatenciesEnd + (NetworkConfig.RttWindowSize - 1)) % NetworkConfig.RttWindowSize];
            }
            else
            {
                ret.AverageSec = 0;
                ret.BestSec = 0;
                ret.WorstSec = 0;
                ret.SampleCount = 0;
                ret.LastSec = 0;
            }

            return ret;
        }

        internal void NotifySend(int sequence, double timeSec)
        {
            m_RttSendTimes[sequence % NetworkConfig.RttWindowSize] = timeSec;
            m_SendSequence[sequence % NetworkConfig.RttWindowSize] = sequence;
        }

        internal void NotifyAck(int sequence, double timeSec)
        {
            // if the same slot was not used by a later send
            if (m_SendSequence[sequence % NetworkConfig.RttWindowSize] == sequence)
            {
                double latency = timeSec - m_RttSendTimes[sequence % NetworkConfig.RttWindowSize];

                m_MeasuredLatencies[m_LatenciesEnd] = latency;
                m_LatenciesEnd = (m_LatenciesEnd + 1) % NetworkConfig.RttAverageSamples;

                if (m_LatenciesEnd == m_LatenciesBegin)
                {
                    m_LatenciesBegin = (m_LatenciesBegin + 1) % NetworkConfig.RttAverageSamples;
                }
            }
        }
    }
}
