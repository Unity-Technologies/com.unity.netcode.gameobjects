using UnityEngine;

namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// Can be used independently or assigned to <see cref="NetcodeIntegrationTest.WaitForConditionOrTimeOut"/> in the
    /// event the default timeout period needs to be adjusted
    /// </summary>
    public class TimeoutHelper
    {
        /// <summary>
        /// The default time out value.
        /// </summary>
        protected const float k_DefaultTimeOutWaitPeriod = 2.0f;

        private float m_MaximumTimeBeforeTimeOut;
        private float m_TimeOutPeriod;

        /// <summary>
        /// <see cref="true"/> If a timed conditional check has started and <see cref="false"/> if it has yet to start.
        /// </summary>
        protected bool m_IsStarted { get; private set; }

        /// <summary>
        /// Will be <see cref="true"/> if a timeout occurred or <see cref="false"/> if a timeout did not occur.
        /// </summary>
        public bool TimedOut { get; internal set; }

        private float m_TimeStarted;
        private float m_TimeStopped;

        /// <summary>
        /// Gets the time that has passed since started.
        /// </summary>
        /// <returns><see cref="float"/> as the time that has passed.</returns>
        public float GetTimeElapsed()
        {
            if (m_IsStarted)
            {
                return Time.realtimeSinceStartup - m_TimeStarted;
            }
            else
            {
                return m_TimeStopped - m_TimeStarted;
            }
        }

        /// <summary>
        /// Virtual method to override in order to setup a derived class when started.
        /// </summary>
        protected virtual void OnStart()
        {
        }

        /// <summary>
        /// Invoke to start.
        /// </summary>
        public void Start()
        {
            m_TimeStopped = 0.0f;
            m_TimeStarted = Time.realtimeSinceStartup;
            m_MaximumTimeBeforeTimeOut = Time.realtimeSinceStartup + m_TimeOutPeriod;
            m_IsStarted = true;
            TimedOut = false;
            OnStart();
        }

        /// <summary>
        /// Virtual method to override in order to clean up or other related logic when stopped.
        /// </summary>
        protected virtual void OnStop()
        {
        }

        /// <summary>
        /// Invoke to stop.
        /// </summary>
        public void Stop()
        {
            if (m_TimeStopped == 0.0f)
            {
                m_TimeStopped = Time.realtimeSinceStartup;
            }
            TimedOut = HasTimedOut();
            m_IsStarted = false;
            OnStop();
        }

        /// <summary>
        /// Virtual method returns <see cref="true"/> or <see cref="false"/> if timed out.
        /// </summary>
        /// <remarks>
        /// Overriding this provides additional conditions to determine if it has timed out.
        /// </remarks>
        /// <returns><see cref="true"/> or <see cref="false"/></returns>
        protected virtual bool OnHasTimedOut()
        {
            return m_IsStarted ? m_MaximumTimeBeforeTimeOut < Time.realtimeSinceStartup : TimedOut;
        }

        /// <summary>
        /// Invoke to determine if it has timed out.
        /// </summary>
        /// <returns><see cref="true"/> or <see cref="false"/></returns>
        public bool HasTimedOut()
        {
            return OnHasTimedOut();
        }

        /// <summary>
        /// Constructor that provides an optional <see cref="float"/> parameter to define the time out period.
        /// </summary>
        /// <param name="timeOutPeriod">Optional <see cref="float"/> to define the time out period. It defaults to <see cref="k_DefaultTimeOutWaitPeriod"/>.</param>
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
        private int m_StartFrameCount;
        private int m_EndFrameCount;
        private bool m_ReachedFrameCount;

        /// <summary>
        /// Returns the number of frames that have occurred.
        /// </summary>
        /// <returns>Number of frames as an <see cref="int"/>.</returns>
        public int GetFrameCount()
        {
            if (m_IsStarted)
            {
                return Time.frameCount - m_StartFrameCount;
            }
            else
            {
                return m_EndFrameCount - m_StartFrameCount;
            }
        }

        ///<inheritdoc/>
        protected override void OnStop()
        {
            if (m_EndFrameCount == 0)
            {
                m_EndFrameCount = Time.frameCount;
            }
            base.OnStop();
        }

        ///<inheritdoc/>
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

        ///<inheritdoc/>
        protected override void OnStart()
        {
            m_EndFrameCount = 0;
            m_StartFrameCount = Time.frameCount;
            base.OnStart();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeOutPeriod">Optional timeout period.</param>
        /// <param name="tickRate">Optional tick rate.</param>
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
