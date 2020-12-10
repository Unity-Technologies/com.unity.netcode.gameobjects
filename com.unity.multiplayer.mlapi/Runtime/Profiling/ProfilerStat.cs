using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.Serialization;

namespace MLAPI.Profiling
{
   public struct Sample
   {
      public int count;
      public float t_recorded;
   }

   public class ProfilerStat
   {
        public ProfilerStat(string name)
        {
            prettyPrintName = name;
            ProfilerStatManager.Add(this);
        }

        public string prettyPrintName;
        protected int maxSamples = 10;

        protected LinkedList<Sample> data = new LinkedList<Sample>();

        private bool dirty = true;

        protected float lastCount;
        protected float lastT;

        public virtual void Record(int amt = 1)
        {
            dirty = true;
            var t_now = Time.time;
        // 'Record' can get called many times in the same frame (for the same exact timestamp)
        //   This not only blows out the samples but makes the rate computation break since we 
        //   have n samples with a time delta of zero.
        // 
        //   Instead, if we want to record a value at the same exact time as our last
        //   sample, just adjust that sample
            if (data.First != null && data.First.Value.t_recorded == t_now)
            {
                data.First.Value = new Sample() {count = data.First.Value.count + amt,
                t_recorded = data.First.Value.t_recorded};
            }
            else
            {
                data.AddFirst(new Sample() {count = amt, t_recorded = Time.time});
                while (data.Count > maxSamples)
                {
                    data.RemoveLast();
                }
            }
        }

        public virtual float SampleRate()
        {
            if (dirty)
            {
                LinkedListNode<Sample> node = data.First;
                lastCount = 0;
                lastT = (data.Last != null) ? data.Last.Value.t_recorded : 0.0f;

                while (node != null)
                {
                lastCount += node.Value.count;
                node = node.Next;
                }
                dirty = false;
            }

            float delta = Time.time - lastT;
            if (delta == 0.0f)
            {
                return 0.0f;
            }

            return lastCount / delta;
        }
    }

    public class ProfilerIncStat : ProfilerStat
    {
        public ProfilerIncStat(string name) : base(name) { }

        private float internalValue = 0f;

        public override void Record(int amt = 1)
        {
            data.AddFirst(new Sample() { count = amt, t_recorded = Time.time });
            while (data.Count > maxSamples) {
                data.RemoveLast();
            }

            internalValue += amt;
        }

        public override float SampleRate()
        {
            return internalValue;
        }
    }
}
