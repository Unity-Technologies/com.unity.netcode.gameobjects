using System.Collections;
using UnityEngine;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Base class used to handle processing smoke tests
    /// </summary>
    public class SmokeTestState
    {
        public enum StateStatus
        {
            Stopped,
            Starting,
            Processing,
            Stopping,
        }

        public StateStatus CurrentState { get; internal set; }

        private delegate IEnumerator StateProcessorDelegateHandler();

        protected float m_ProcessWaitTime = 0.1f;

        public void Start()
        {
            if (CurrentState == StateStatus.Stopped)
            {
                CurrentState = StateStatus.Starting;
            }
        }

        public string GetStateName()
        {
            return GetType().Name;
        }

        protected virtual IEnumerator OnStartState()
        {
            CurrentState = StateStatus.Processing;

            yield break;
        }

        private IEnumerator StartState()
        {
            Debug.Log($"Starting {GetStateName()}.");
            return OnStartState();
        }

        protected virtual bool OnProcessState()
        {
            return false;
        }

        private IEnumerator ProcessState()
        {
            Debug.Log($"Processing {GetStateName()}.");
            if (!OnProcessState())
            {
                CurrentState = StateStatus.Stopping;
                yield break;
            }
            yield return new WaitForSeconds(m_ProcessWaitTime);
        }

        public IEnumerator UpdateState()
        {
            switch (CurrentState)
            {
                case StateStatus.Starting:
                    {
                        yield return StartState();
                        break;
                    }
                case StateStatus.Processing:
                    {
                        yield return ProcessState();
                        break;
                    }
                case StateStatus.Stopping:
                    {
                        yield return StopState();
                        break;
                    }
            }
        }

        protected virtual IEnumerator OnStopState()
        {
            CurrentState = StateStatus.Stopped;
            yield break;
        }

        private IEnumerator StopState()
        {
            Debug.Log($"Stopping {GetStateName()}.");
            return OnStopState();
        }
    }
}
