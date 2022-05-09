using UnityEngine;

namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// Can be used independently or assigned to <see cref="NetcodeIntegrationTest.WaitForConditionOrTimeOut"></see> in the
    /// event the default timeout period needs to be adjusted
    /// </summary>
    public class TimeoutHelper
    {
        private const float k_DefaultTimeOutWaitPeriod = 2.0f;

        private float m_MaximumTimeBeforeTimeOut;
        private float m_TimeOutPeriod;

        private bool m_IsStarted;
        public bool TimedOut { get; internal set; }

        public void Start()
        {
            m_MaximumTimeBeforeTimeOut = Time.realtimeSinceStartup + m_TimeOutPeriod;
            m_IsStarted = true;
            TimedOut = false;
        }

        public void Stop()
        {
            TimedOut = HasTimedOut();
            m_IsStarted = false;
        }

        public bool HasTimedOut()
        {
            return m_IsStarted ? m_MaximumTimeBeforeTimeOut < Time.realtimeSinceStartup : TimedOut;
        }

        public TimeoutHelper(float timeOutPeriod = k_DefaultTimeOutWaitPeriod)
        {
            m_TimeOutPeriod = timeOutPeriod;
        }
    }
}
