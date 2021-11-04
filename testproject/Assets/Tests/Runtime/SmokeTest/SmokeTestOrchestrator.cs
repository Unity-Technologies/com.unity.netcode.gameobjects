using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Used for all smoke tests
    /// </summary>
    public class SmokeTestOrchestrator : MonoBehaviour
    {
        public delegate void OnStateFinishedProcessingDelegateHandler(float timeToProcess);
        public event OnStateFinishedProcessingDelegateHandler OnStateFinishedProcessing;

        private const float k_TimeOutPeriod = 30.0f;

        public SmokeTestState StateBeingProcessed { get; internal set; }

        // Start is called before the first frame update
        private void Start()
        {
            // Move into the DDOL scene
            DontDestroyOnLoad(this);
        }

        public void SetState(SmokeTestState smokeTestState, float timeOut = k_TimeOutPeriod, bool startState = true)
        {
            if (StateBeingProcessed == null || (StateBeingProcessed != null && StateBeingProcessed.CurrentState == SmokeTestState.StateStatus.Stopped))
            {
                StateBeingProcessed = smokeTestState;
                if (startState)
                {
                    smokeTestState.Start();
                    StartCoroutine(StateProcessor(timeOut));
                }
            }
            else
            {
                Debug.LogWarning($"Trying to start a new state {smokeTestState.GetStateName()} when state {StateBeingProcessed.GetStateName()} is still running!");
            }
        }

        private IEnumerator StateProcessor(float timeOut)
        {
            float stateTimedOut = Time.realtimeSinceStartup + timeOut;
            if (StateBeingProcessed != null)
            {
                float timeStarted = Time.realtimeSinceStartup;
                do
                {
                    // Check to make sure we haven't timed out
                    Assert.That(Time.realtimeSinceStartup < stateTimedOut);
                    if (Time.realtimeSinceStartup >= stateTimedOut)
                    {
                        break;
                    }
                    yield return StateBeingProcessed.UpdateState();
                } while (StateBeingProcessed.CurrentState != SmokeTestState.StateStatus.Stopped);
                OnStateFinishedProcessing?.Invoke(Time.realtimeSinceStartup - timeStarted);
            }
            yield break;
        }
    }
}
