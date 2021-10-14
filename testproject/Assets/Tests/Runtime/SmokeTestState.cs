using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;


namespace TestProject.RuntimeTests
{
    public class SmokeTests
    {
        private GameObject m_SmokeTestGameObject;
        private SmokeTestOrchestrator m_SmokeTestOrchestrator;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            m_SmokeTestGameObject = new GameObject();
            m_SmokeTestOrchestrator = m_SmokeTestGameObject.AddComponent<SmokeTestOrchestrator>();

            yield break;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            yield break;
        }

        /// <summary>
        /// Tests that a SmokeTestState derived class will process through
        /// the three states (Starting, Processing, and Stopping)
        /// </summary>
        [UnityTest]
        public IEnumerator SmokeTestStateTest()
        {
            m_SmokeTestOrchestrator.SetState(new TestSmokeTestState());

            while (m_SmokeTestOrchestrator.StateBeingProcessed.CurrentState != SmokeTestState.StateStatus.Stopped)
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }


        [UnityTest]
        public IEnumerator RegisteredScenesValidation()
        {
            m_SmokeTestOrchestrator.SetState(new RegisteredScenesSmokeTest());
            while (m_SmokeTestOrchestrator.StateBeingProcessed.CurrentState != SmokeTestState.StateStatus.Stopped)
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }
    }



    public class TestSmokeTestState : SmokeTestState
    {
        protected override IEnumerator OnStartState()
        {
            Debug.Log($"Starting {nameof(TestSmokeTestState)}.");
            return base.OnStartState();
        }

        protected override bool OnProcessState()
        {
            Debug.Log($"Processing {nameof(TestSmokeTestState)}.");
            return base.OnProcessState();
        }

        protected override IEnumerator OnStopState()
        {
            Debug.Log($"Stopping {nameof(TestSmokeTestState)}.");
            return base.OnStopState();
        }
    }
}
