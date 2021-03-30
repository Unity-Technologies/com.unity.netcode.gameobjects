using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.Profiling
{
    public struct Sample
    {
        public int Count;
        public float TimeRecorded;
    }

    public class ProfilerStat
    {
        public ProfilerStat(string name)
        {
            PrettyPrintName = name;
            ProfilerStatManager.Add(this);
        }

        public string PrettyPrintName;
        protected int m_MaxSamples = 10;

        protected LinkedList<Sample> m_Data = new LinkedList<Sample>();

        private bool m_IsDirty = true;

        protected float m_LastCount;
        protected float m_LastTime;

        public virtual void Record(int amt = 1)
        {
            m_IsDirty = true;
            var timeNow = Time.time;
            // 'Record' can get called many times in the same frame (for the same exact timestamp)
            //   This not only blows out the samples but makes the rate computation break since we 
            //   have n samples with a time delta of zero.
            // 
            //   Instead, if we want to record a value at the same exact time as our last
            //   sample, just adjust that sample
            if (m_Data.First != null && m_Data.First.Value.TimeRecorded == timeNow)
            {
                m_Data.First.Value = new Sample()
                {
                    Count = m_Data.First.Value.Count + amt,
                    TimeRecorded = m_Data.First.Value.TimeRecorded
                };
            }
            else
            {
                m_Data.AddFirst(new Sample() { Count = amt, TimeRecorded = Time.time });
                while (m_Data.Count > m_MaxSamples)
                {
                    m_Data.RemoveLast();
                }
            }
        }

        public virtual float SampleRate()
        {
            if (m_IsDirty)
            {
                LinkedListNode<Sample> node = m_Data.First;
                m_LastCount = 0;
                m_LastTime = m_Data.Last?.Value.TimeRecorded ?? 0.0f;

                while (node != null)
                {
                    m_LastCount += node.Value.Count;
                    node = node.Next;
                }

                m_IsDirty = false;
            }

            float delta = Time.time - m_LastTime;
            if (delta == 0.0f)
            {
                return 0.0f;
            }

            return m_LastCount / delta;
        }
    }

    public class ProfilerIncStat : ProfilerStat
    {
        public ProfilerIncStat(string name) : base(name) { }

        private float m_InternalValue = 0f;

        public override void Record(int amt = 1)
        {
            m_Data.AddFirst(new Sample() { Count = amt, TimeRecorded = Time.time });
            while (m_Data.Count > m_MaxSamples)
            {
                m_Data.RemoveLast();
            }

            m_InternalValue += amt;
        }

        public override float SampleRate()
        {
            return m_InternalValue;
        }
    }
}
