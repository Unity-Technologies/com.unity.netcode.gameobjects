using UnityEngine;

namespace Unity.Netcode
{
    internal class ProfilerStat
    {
        public ProfilerStat(string name)
        {
            PrettyPrintName = name;
            ProfilerStatManager.Add(this);
        }

        public string PrettyPrintName;

        private int m_CurrentSecond = 0;
        private int m_TotalAmount = 0;
        private int m_Sample = 0;

        public virtual void Record(int amt = 1)
        {
            int currentSecond = (int)Time.unscaledTime;
            if (currentSecond != m_CurrentSecond)
            {
                m_Sample = m_TotalAmount;

                m_TotalAmount = 0;
                m_CurrentSecond = currentSecond;
            }

            m_TotalAmount += amt;
        }

        public virtual float SampleRate()
        {
            return m_Sample;
        }
    }

    internal class ProfilerIncStat : ProfilerStat
    {
        public ProfilerIncStat(string name) : base(name) { }

        private float m_InternalValue = 0f;

        public override void Record(int amt = 1)
        {
            m_InternalValue += amt;
        }

        public override float SampleRate()
        {
            return m_InternalValue;
        }
    }
}
