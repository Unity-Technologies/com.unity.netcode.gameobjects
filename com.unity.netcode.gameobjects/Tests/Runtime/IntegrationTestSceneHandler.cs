using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    internal class IntegrationTestSceneHandler : ISceneManagerHandler, IDisposable
    {
        internal CoroutineRunner CoroutineRunner;

        protected const float k_ClientLoadingSimulatedDelay = 0.02f;
        protected float m_ClientLoadingSimulatedDelay = k_ClientLoadingSimulatedDelay;

        public delegate bool CanClientsLoadUnloadDelegateHandler();
        public event CanClientsLoadUnloadDelegateHandler CanClientsLoad;
        public event CanClientsLoadUnloadDelegateHandler CanClientsUnload;

        internal List<Coroutine> CoroutinesRunning = new List<Coroutine>();

        protected bool OnCanClientsLoad()
        {
            if (CanClientsLoad != null)
            {
                return CanClientsLoad.Invoke();
            }
            return true;
        }

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
