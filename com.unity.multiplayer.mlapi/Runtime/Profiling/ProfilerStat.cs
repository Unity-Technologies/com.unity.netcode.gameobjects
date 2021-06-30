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
        protected int MaxSamples = 10;

        protected LinkedList<Sample> Data = new LinkedList<Sample>();

        private bool m_IsDirty = true;

        protected float LastCount;
        protected float LastTime;

        public virtual void Record(int amt = 1)
        {
            m_IsDirty = true;
            var t_now = Time.time;
            // 'Record' can get called many times in the same frame (for the same exact timestamp)
            //   This not only blows out the samples but makes the rate computation break since we 
            //   have n samples with a time delta of zero.
            // 
            //   Instead, if we want to record a value at the same exact time as our last
            //   sample, just adjust that sample
            if (Data.First != null && Data.First.Value.TimeRecorded == t_now)
            {
                Data.First.Value = new Sample()
                {
                    Count = Data.First.Value.Count + amt,
                    TimeRecorded = Data.First.Value.TimeRecorded
                };
            }
            else
            {
                Data.AddFirst(new Sample() { Count = amt, TimeRecorded = Time.time });
                while (Data.Count > MaxSamples)
                {
                    Data.RemoveLast();
                }
            }
        }

        public virtual float SampleRate()
        {
            if (m_IsDirty)
            {
                LinkedListNode<Sample> node = Data.First;
                LastCount = 0;
                LastTime = Data.Last?.Value.TimeRecorded ?? 0.0f;

                while (node != null)
                {
                    LastCount += node.Value.Count;
                    node = node.Next;
                }

                m_IsDirty = false;
            }

            float delta = Time.time - LastTime;
            if (delta == 0.0f)
            {
                return 0.0f;
            }

            return LastCount / delta;
        }
    }

    public class ProfilerIncStat : ProfilerStat
    {
        public ProfilerIncStat(string name) : base(name) { }

        private float m_InternalValue = 0f;

        public override void Record(int amt = 1)
        {
            Data.AddFirst(new Sample() { Count = amt, TimeRecorded = Time.time });
            while (Data.Count > MaxSamples)
            {
                Data.RemoveLast();
            }

            m_InternalValue += amt;
        }

        public override float SampleRate()
        {
            return m_InternalValue;
        }
    }
}