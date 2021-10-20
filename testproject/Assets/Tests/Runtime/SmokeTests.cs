using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public class SmokeTests
    {
        public enum DebugLevel
        {
            NONE,
            NORMAL,
            VERBOSE
        }

        public static DebugLevel DebugVerbosity = DebugLevel.NONE;
        private GameObject m_SmokeTestGameObject;
        private SmokeTestOrchestrator m_SmokeTestOrchestrator;
        private List<List<string>> m_RegisteredSceneReferences;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_RegisteredSceneReferences = new List<List<string>>();
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
        [Order(1)]
        public IEnumerator SmokeTestStateTest()
        {
            m_SmokeTestOrchestrator.SetState(new TestSmokeTestState());

            while (m_SmokeTestOrchestrator.StateBeingProcessed.CurrentState != SmokeTestState.StateStatus.Stopped)
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }

        /// <summary>
        /// Updates the global m_RegisteredSceneReferences.
        /// These are stored as SceneReference Groups:
        ///     A SceneReference Group is the list of all scenes referenced within a given SceneReference asset
        ///     The first scene is always the "master scene" which will contain the in-scene placed GameObject with
        ///     NetworkManager component.
        /// </summary>
        private void RegisteredScenesSmokeTest_OnCollectedRegisteredScenes(List<List<string>> registeredSceneNames)
        {
            m_RegisteredSceneReferences.AddRange(registeredSceneNames);
        }

        /// <summary>
        /// Validates that all menu references and the scenes that each menu references
        /// are valid, registered with the Build Settings Scenes in Build, and this builds
        /// a global list of registered scene references to be used by the SceneReferenceValidation
        /// </summary>
        [UnityTest]
        [Order(2)]
        public IEnumerator RegisteredSceneValidation()
        {
            var registeredScenesSmokeTest = new RegisteredSceneValidationState();
            registeredScenesSmokeTest.OnCollectedRegisteredScenes += RegisteredScenesSmokeTest_OnCollectedRegisteredScenes;
            m_SmokeTestOrchestrator.SetState(registeredScenesSmokeTest);
            while (m_SmokeTestOrchestrator.StateBeingProcessed.CurrentState != SmokeTestState.StateStatus.Stopped)
            {
                yield return new WaitForSeconds(0.1f);
            }

            DebugRegisteredSceneReferences();

            yield break;
        }

        /// <summary>
        /// Primarily used for debugging and sanity check.
        /// </summary>
        private void DebugRegisteredSceneReferences()
        {
            if (DebugVerbosity == DebugLevel.NONE)
            {
                return;
            }

            var scenesReferenced = "Scenes Referenced:\n";
            foreach (var sceneGroup in m_RegisteredSceneReferences)
            {
                if (sceneGroup.Count > 1)
                {
                    scenesReferenced += $"SceneGroup [{sceneGroup[0]}]\n";
                    foreach (var sceneName in sceneGroup)
                    {
                        if (sceneName == sceneGroup[0])
                        {
                            continue;
                        }
                        scenesReferenced += $"{sceneName}\n";
                    }
                }
                else
                {
                    scenesReferenced += $"SceneGroup [{sceneGroup[0]}] : {sceneGroup[0]}\n";
                }
            }
            Debug.Log(scenesReferenced);
        }

        /// <summary>
        /// Does a very basic check to make sure each master scene within
        /// a SceneReference asset can be loaded and the NetworkManager
        /// can be started.
        /// </summary>
        [UnityTest]
        [Order(3)]
        public IEnumerator SceneReferenceValidation()
        {
            var sceneLoadingValidationState = new SceneReferenceValidationState();
            foreach (var sceneGroup in m_RegisteredSceneReferences)
            {
                sceneLoadingValidationState.SetScenes(sceneGroup);
                m_SmokeTestOrchestrator.SetState(sceneLoadingValidationState);
                while (m_SmokeTestOrchestrator.StateBeingProcessed.CurrentState != SmokeTestState.StateStatus.Stopped)
                {
                    yield return new WaitForSeconds(0.1f);
                }
                yield return new WaitForSeconds(0.1f);
            }
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
