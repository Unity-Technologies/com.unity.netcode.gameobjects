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
    /// Original class -- will be removed if the new IntegrationTestSceneHandler cannot load
    /// scenes for all clients.
    /// </summary>
    internal class OldIntegrationTestSceneHandler : ISceneManagerHandler, IDisposable
    {
        internal CoroutineRunner CoroutineRunner;

        // Default client simulated delay time
        protected const float k_ClientLoadingSimulatedDelay = 0.016f;

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

        public OldIntegrationTestSceneHandler()
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

    internal class IntegrationTestSceneHandler : ISceneManagerHandler, IDisposable
    {
        internal static CoroutineRunner CoroutineRunner;

        // Default client simulated delay time
        protected const float k_ClientLoadingSimulatedDelay = 0.006f;

        // Controls the client simulated delay time
        protected static float s_ClientLoadingSimulatedDelay = k_ClientLoadingSimulatedDelay;


        internal static List<QueuedSceneJob> QueuedSceneJobs = new List<QueuedSceneJob>();
        internal List<Coroutine> CoroutinesRunning = new List<Coroutine>();
        internal static Coroutine SceneJobProcessor;
        internal static QueuedSceneJob CurrentQueuedSceneJob;
        protected static WaitForSeconds s_WaitForSeconds;


        public delegate bool CanClientsLoadUnloadDelegateHandler();
        public static event CanClientsLoadUnloadDelegateHandler CanClientsLoad;
        public static event CanClientsLoadUnloadDelegateHandler CanClientsUnload;

        internal class QueuedSceneJob
        {
            public enum JobTypes
            {
                Loading,
                Unloading,
                Completed
            }
            public JobTypes JobType;
            public string SceneName;
            public Scene Scene;
            public ISceneManagerHandler.SceneEventAction SceneAction;
            public IntegrationTestSceneHandler IntegrationTestSceneHandler;
        }

        internal NetworkManager NetworkManager;

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

        protected bool OnCanClientsUnload()
        {
            if (CanClientsUnload != null)
            {
                return CanClientsUnload.Invoke();
            }
            return true;
        }

        static internal IEnumerator ProcessLoadingSceneJob(QueuedSceneJob queuedSceneJob)
        {
            var itegrationTestSceneHandler = queuedSceneJob.IntegrationTestSceneHandler;
            yield return s_WaitForSeconds;
            while (!itegrationTestSceneHandler.OnCanClientsLoad())
            {
                yield return s_WaitForSeconds;
            }

            //SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            //// We always load additively for all scenes during integration tests
            //SceneManager.LoadSceneAsync(queuedSceneJob.SceneName, LoadSceneMode.Additive);

            CurrentQueuedSceneJob.SceneAction.Invoke();
            CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;

            // Wait for it to finish
            while (queuedSceneJob.JobType != QueuedSceneJob.JobTypes.Completed)
            {
                yield return s_WaitForSeconds;
            }
        }


        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (CurrentQueuedSceneJob.SceneName == scene.name)
            {
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                //scene.name += $"-OnClient({CurrentQueuedSceneJob.IntegrationTestSceneHandler.NetworkManager.LocalClientId})";
                CurrentQueuedSceneJob.SceneAction.Invoke();
                CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;
            }
        }

        static internal IEnumerator ProcessUnloadingSceneJob(QueuedSceneJob queuedSceneJob)
        {
            var itegrationTestSceneHandler = queuedSceneJob.IntegrationTestSceneHandler;
            yield return s_WaitForSeconds;
            while (!itegrationTestSceneHandler.OnCanClientsUnload())
            {
                yield return s_WaitForSeconds;
            }

            //SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
            //if (queuedSceneJob.Scene.IsValid() && queuedSceneJob.Scene.isLoaded)
            //{
            //    SceneManager.UnloadSceneAsync(queuedSceneJob.Scene);
            //}
            //else
            //{
            //    CurrentQueuedSceneJob.SceneAction.Invoke();
            //    CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;
            //}

            CurrentQueuedSceneJob.SceneAction.Invoke();
            CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;
            // Wait for it to finish
            while (queuedSceneJob.JobType != QueuedSceneJob.JobTypes.Completed)
            {
                yield return s_WaitForSeconds;
            }
        }

        private static void SceneManager_sceneUnloaded(Scene scene)
        {
            if (CurrentQueuedSceneJob.Scene.name == scene.name)
            {
                SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
                CurrentQueuedSceneJob.SceneAction.Invoke();
                CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;
            }
        }

        static internal IEnumerator JobQueueProcessor()
        {
            var nextJob = QueuedSceneJobs[0];
            while (nextJob != null)
            {
                CurrentQueuedSceneJob = nextJob;
                if (nextJob.JobType == QueuedSceneJob.JobTypes.Loading)
                {
                    yield return ProcessLoadingSceneJob(nextJob);
                }
                else if (nextJob.JobType == QueuedSceneJob.JobTypes.Unloading)
                {
                    yield return ProcessUnloadingSceneJob(nextJob);
                }
                QueuedSceneJobs.Remove(nextJob);
                nextJob = QueuedSceneJobs.Count > 0 ? QueuedSceneJobs[0] : null;
            }
            SceneJobProcessor = null;
            yield break;
        }

        private void AddJobToQueue(QueuedSceneJob queuedSceneJob)
        {
            QueuedSceneJobs.Add(queuedSceneJob);
            if (SceneJobProcessor == null)
            {
                SceneJobProcessor = CoroutineRunner.StartCoroutine(JobQueueProcessor());
            }
        }

        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            AddJobToQueue(new QueuedSceneJob() { IntegrationTestSceneHandler = this, SceneName = sceneName, SceneAction = sceneEventAction, JobType = QueuedSceneJob.JobTypes.Loading });
            // This is OK to return a "nothing" AsyncOperation since we are simulating client loading
            return new AsyncOperation();
        }

        public AsyncOperation UnloadSceneAsync(Scene scene, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            AddJobToQueue(new QueuedSceneJob() { IntegrationTestSceneHandler = this, Scene = scene, SceneAction = sceneEventAction, JobType = QueuedSceneJob.JobTypes.Unloading });
            // This is OK to return a "nothing" AsyncOperation since we are simulating client loading
            return new AsyncOperation();
        }

        internal Scene GetAndAddNewlyLoadedSceneByName(string sceneName)
        {
            var otherNetworkManagers = NetworkManagers;
            otherNetworkManagers.Remove(NetworkManager);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sceneLoaded = SceneManager.GetSceneAt(i);
                if (sceneLoaded.name == sceneName)
                {
                    var skip = false;
                    foreach (var otherNetworkManager in otherNetworkManagers)
                    {
                        if (otherNetworkManager.SceneManager.ScenesLoaded.ContainsKey(sceneLoaded.handle))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                    {
                        continue;
                    }

                    if (!NetworkManager.SceneManager.ScenesLoaded.ContainsKey(sceneLoaded.handle))
                    {
                        NetworkManager.SceneManager.ScenesLoaded.Add(sceneLoaded.handle, sceneLoaded);
                        return sceneLoaded;
                    }
                }
            }

            throw new Exception($"Failed to find any loaded scene named {sceneName}!");
        }


        internal static List<NetworkManager> NetworkManagers = new List<NetworkManager>();

        public IntegrationTestSceneHandler(NetworkManager networkManager)
        {
            networkManager.SceneManager.OverrideGetAndAddNewlyLoadedSceneByName = GetAndAddNewlyLoadedSceneByName;
            NetworkManagers.Add(networkManager);
            if (s_WaitForSeconds == null)
            {
                s_WaitForSeconds = new WaitForSeconds(s_ClientLoadingSimulatedDelay);
            }
            NetworkManager = networkManager;
            if (CoroutineRunner == null)
            {
                CoroutineRunner = new GameObject("UnitTestSceneHandlerCoroutine").AddComponent<CoroutineRunner>();
            }
        }

        public void Dispose()
        {
            NetworkManagers.Clear();
            if (SceneJobProcessor != null)
            {
                CoroutineRunner.StopCoroutine(SceneJobProcessor);
            }
            CoroutineRunner.StopAllCoroutines();

            foreach (var job in QueuedSceneJobs)
            {
                if (job.JobType != QueuedSceneJob.JobTypes.Completed)
                {
                    if (job.JobType == QueuedSceneJob.JobTypes.Loading)
                    {
                        SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                    }
                    else
                    {
                        SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
                    }
                    job.JobType = QueuedSceneJob.JobTypes.Completed;
                }
            }
            QueuedSceneJobs.Clear();
            Object.Destroy(CoroutineRunner.gameObject);
        }
    }
}
