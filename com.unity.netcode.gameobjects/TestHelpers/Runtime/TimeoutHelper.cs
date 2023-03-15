using UnityEngine;

namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// Can be used independently or assigned to <see cref="NetcodeIntegrationTest.WaitForConditionOrTimeOut"></see> in the
    /// event the default timeout period needs to be adjusted
    /// </summary>
    public class TimeoutHelper
    {
        protected const float k_DefaultTimeOutWaitPeriod = 2.0f;


        private float m_MaximumTimeBeforeTimeOut;
        private float m_TimeOutPeriod;

        protected bool m_IsStarted { get; private set; }
        public bool TimedOut { get; internal set; }

        protected virtual void OnStart()
        {
        }

        public void Start()
        {
            m_MaximumTimeBeforeTimeOut = Time.realtimeSinceStartup + m_TimeOutPeriod;
            m_IsStarted = true;
            TimedOut = false;
            OnStart();
        }

        protected virtual void OnStop()
        {
        }

        public void Stop()
        {
            TimedOut = HasTimedOut();
            m_IsStarted = false;
            OnStop();
        }

        protected virtual bool OnHasTimedOut()
        {
            return m_IsStarted ? m_MaximumTimeBeforeTimeOut < Time.realtimeSinceStartup : TimedOut;
        }

        public bool HasTimedOut()
        {
            return OnHasTimedOut();
        }

        public TimeoutHelper(float timeOutPeriod = k_DefaultTimeOutWaitPeriod)
        {
            m_TimeOutPeriod = timeOutPeriod;
        }
    }

    /// <summary>
    /// This can be used in place of TimeoutHelper if you suspect a test is having
    /// issues on a system where the frame rate is running slow than expected and
    /// allowing a certain number of frame updates is required.
    /// </summary>
    public class TimeoutFrameCountHelper : TimeoutHelper
    {
        private const uint k_DefaultTickRate = 30;

        private float m_TotalFramesToWait;
        private float m_StartFrameCount;
        private bool m_ReachedFrameCount;

        protected override bool OnHasTimedOut()
        {
            var currentFrameCountDelta = Time.frameCount - m_StartFrameCount;
            if (m_IsStarted)
            {
                m_ReachedFrameCount = currentFrameCountDelta >= m_TotalFramesToWait;
            }
            // Only time out if we have both exceeded the time period and the expected number of frames has reached the expected number of frames
            // (this handles the scenario where some systems are running a much lower frame rate)
            return m_ReachedFrameCount && base.OnHasTimedOut();
        }

        protected override void OnStart()
        {
            m_StartFrameCount = Time.frameCount;
            base.OnStart();
        }

        public TimeoutFrameCountHelper(float timeOutPeriod = k_DefaultTimeOutWaitPeriod, uint tickRate = k_DefaultTickRate) : base(timeOutPeriod)
        {
            // Calculate the expected number of frame updates that should occur during the tick count wait period
            var frameFrequency = 1.0f / (Application.targetFrameRate >= 60 && Application.targetFrameRate <= 100 ? Application.targetFrameRate : 60.0f);
            var tickFrequency = 1.0f / tickRate;
            var framesPerTick = tickFrequency / frameFrequency;
            var totalExpectedTicks = timeOutPeriod / tickFrequency;

            m_TotalFramesToWait = framesPerTick * totalExpectedTicks;
        }
    }
}
