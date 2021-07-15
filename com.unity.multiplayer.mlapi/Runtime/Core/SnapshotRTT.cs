using System;
using UnityEngine;

namespace MLAPI
{
    internal class ClientRtt
    {
        /// <summary>
        /// Round-trip-time data
        /// </summary>
        public struct Rtt
        {
            public double Best;
            public double Average;
            public double Worst;
            public int SampleCount;
        }
        public ClientRtt()
        {
            m_RttSendTimes = new double[k_RingSize];
            m_SendKey = new int[k_RingSize];
            m_MeasuredLatencies = new double[k_RingSize];
        }

        /// <summary>
        /// Returns the Round-trip-time computation for this client
        /// </summary>
        public Rtt GetRtt()
        {
            var ret = new Rtt(); // is this a memory alloc ? How do I get a stack alloc ?
            var index = m_LatenciesBegin;
            double total = 0.0;
            ret.Best = m_MeasuredLatencies[m_LatenciesBegin];
            ret.Worst = m_MeasuredLatencies[m_LatenciesBegin];

            while (index != m_LatenciesEnd)
            {
                total += m_MeasuredLatencies[index];
                ret.SampleCount++;
                ret.Best = Math.Min(ret.Best, m_MeasuredLatencies[index]);
                ret.Worst = Math.Max(ret.Worst, m_MeasuredLatencies[index]);
                index = (index + 1) % k_RttSize;
            }

            if (ret.SampleCount != 0)
            {
                ret.Average = total / ret.SampleCount;
            }
            else
            {
                ret.Average = 0;
                ret.Best = 0;
                ret.Worst = 0;
                ret.SampleCount = 0;
            }

            return ret;
        }

        internal void NotifySend(int key, double when)
        {
            m_RttSendTimes[key % k_RingSize] = when;
            m_SendKey[key % k_RingSize] = key;
        }

        internal void NotifyAck(int key, double when)
        {
            // if the same slot was not used by a later send
            if (m_SendKey[key % k_RingSize] == key)
            {
                double latency = when - m_RttSendTimes[key % k_RingSize];
                Debug.Log(string.Format("Measured latency of {0}", latency));
                m_MeasuredLatencies[m_LatenciesEnd] = latency;
                m_LatenciesEnd = (m_LatenciesEnd + 1) % k_RttSize;

                if (m_LatenciesEnd == m_LatenciesBegin)
                {
                    m_LatenciesBegin = (m_LatenciesBegin + 1) % k_RttSize;
                }
            }
        }

        private const int k_RttSize = 5; // number of RTT to keep an average of (plus one)

        private const int
            k_RingSize = 64; // number of slots to use for RTT computations (max number of in-flight packets)

        private double[] m_RttSendTimes; // times at which packet were sent for RTT computations
        private int[] m_SendKey; // tick (or other key) at which packets were sent (to allow matching)
        private double[] m_MeasuredLatencies; // measured latencies (ring buffer)
        private int m_LatenciesBegin = 0; // ring buffer begin
        private int m_LatenciesEnd = 0; // ring buffer end
    }
}
