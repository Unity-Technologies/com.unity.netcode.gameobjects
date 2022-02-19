using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// The default SceneManagerHandler used for all NetcodeIntegrationTest derived children.
    /// </summary>
    internal class IntegrationTestSceneHandler : ISceneManagerHandler, IDisposable
    {
        internal CoroutineRunner CoroutineRunner;

        // Default client simulated delay time
        protected const float k_ClientLoadingSimulatedDelay = 0.02f;

        // Controls the client simulated delay time
        protected float m_ClientLoadingSimulatedDelay = k_ClientLoadingSimulatedDelay;

        public delegate bool CanClientsLoadUnloadDelegateHandler();
        public event CanClientsLoadUnloadDelegateHandler CanClientsLoad;
        public event CanClientsLoadUnloadDelegateHandler CanClientsUnload;

        internal List<Coroutine> CoroutinesRunning = new List<Coroutine>();

        /// <summary>
        /// Used to control when clients should attempt to fake-load a scene
        /// Note: Unit/Integration tests that only use <see cref="NetcodeIntegrationTestHelpers"/>
        /// need to subscribe to the CanClientsLoad and CanClientsUnload events
        /// in order to control when clients can fake-load.
        /// Tests that derive from <see cref="NetcodeIntegrationTest"/> already have integrated
        /// support and you can override <see cref="NetcodeIntegrationTest.CanClientsLoad"/> and
        /// <see cref="NetcodeIntegrationTest.CanClientsUnload"/>.
        /// </summary>
        protected bool OnCanClientsLoad()
        {
            if (CanClientsLoad != null)
            {
                return CanClientsLoad.Invoke();
            }
            return true;
        }

        /// <summary>
        /// Fake-Loads a scene for a client
        /// </summary>
        internal IEnumerator ClientLoadSceneCoroutine(string sceneName, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            yield return new WaitForSeconds(m_ClientLoadingSimulatedDelay);
            while (!OnCanClientsLoad())
            {
                yield return new WaitForSeconds(m_ClientLoadingSimulatedDelay);
            }
            sceneEventAction.Invoke();
        }

        protected bool OnCanClientsUnload()
        {
            if (CanClientsUnload != null)
            {
                return CanClientsUnload.Invoke();
            }
            return true;
        }

        /// <summary>
        /// Fake-Unloads a scene for a client
        /// </summary>
        internal IEnumerator ClientUnloadSceneCoroutine(ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            yield return new WaitForSeconds(m_ClientLoadingSimulatedDelay);
            while (!OnCanClientsUnload())
            {
                yield return new WaitForSeconds(m_ClientLoadingSimulatedDelay);
            }
            sceneEventAction.Invoke();
        }

        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            CoroutinesRunning.Add(CoroutineRunner.StartCoroutine(ClientLoadSceneCoroutine(sceneName, sceneEventAction)));
            // This is OK to return a "nothing" AsyncOperation since we are simulating client loading
            return new AsyncOperation();
        }

        public AsyncOperation UnloadSceneAsync(Scene scene, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            CoroutinesRunning.Add(CoroutineRunner.StartCoroutine(ClientUnloadSceneCoroutine(sceneEventAction)));
            // This is OK to return a "nothing" AsyncOperation since we are simulating client loading
            return new AsyncOperation();
        }

        public IntegrationTestSceneHandler()
        {
            if (CoroutineRunner == null)
            {
                CoroutineRunner = new GameObject("UnitTestSceneHandlerCoroutine").AddComponent<CoroutineRunner>();
            }
        }

        public void Dispose()
        {
            foreach (var coroutine in CoroutinesRunning)
            {
                CoroutineRunner.StopCoroutine(coroutine);
            }
            CoroutineRunner.StopAllCoroutines();

            Object.Destroy(CoroutineRunner.gameObject);
        }
    }
}
