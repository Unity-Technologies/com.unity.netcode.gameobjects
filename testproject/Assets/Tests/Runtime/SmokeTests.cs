using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;


namespace TestProject.RuntimeTests
{
    public class SmokeTests
    {
        private GameObject m_SmokeTestGameObject;
        private SmokeTestOrchestrator m_SmokeTestOrchestrator;
        private List<string> m_RegisteredSceneReferences;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_RegisteredSceneReferences = new List<string>();
            m_SmokeTestGameObject = new GameObject();
            m_SmokeTestOrchestrator = m_SmokeTestGameObject.AddComponent<SmokeTestOrchestrator>();
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            m_RegisteredSceneReferences.Clear();
            if (m_SmokeTestGameObject != null)
            {
                Object.Destroy(m_SmokeTestGameObject);
            }
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

        private void RegisteredScenesSmokeTest_OnCollectedRegisteredScenes(List<string> registeredSceneNames)
        {
            m_RegisteredSceneReferences.AddRange(registeredSceneNames);
        }

        [UnityTest]
        public IEnumerator RegisteredScenesValidation()
        {
            var registeredScenesSmokeTest = new RegisteredScenesSmokeTest();
            registeredScenesSmokeTest.OnCollectedRegisteredScenes += RegisteredScenesSmokeTest_OnCollectedRegisteredScenes;
            m_SmokeTestOrchestrator.SetState(registeredScenesSmokeTest);
            while (m_SmokeTestOrchestrator.StateBeingProcessed.CurrentState != SmokeTestState.StateStatus.Stopped)
            {
                yield return new WaitForSeconds(0.1f);
            }

            var scenesReferenced = "Scenes Referenced:\n";
            foreach(var sceneName in m_RegisteredSceneReferences)
            {
                scenesReferenced += $"{sceneName}\n";
            }
            Debug.Log(scenesReferenced);
            yield break;
        }
    }


    /// <summary>
    /// Tests the SmokeTestState
    /// </summary>
    public class TestSmokeTestState : SmokeTestState
    {
        protected override IEnumerator OnStartState()
        {
             return base.OnStartState();
        }

        protected override bool OnProcessState()
        {
            return base.OnProcessState();
        }

        protected override IEnumerator OnStopState()
        {
            return base.OnStopState();
        }
    }
}
