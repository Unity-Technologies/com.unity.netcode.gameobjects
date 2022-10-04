using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// The default SceneManagerHandler used for all NetcodeIntegrationTest derived children.
    /// This enables clients to load scenes within the same scene hierarchy during integration
    /// testing.
    /// </summary>
    internal class IntegrationTestSceneHandler : ISceneManagerHandler, IDisposable
    {
        // All IntegrationTestSceneHandler instances register their associated NetworkManager
        internal static List<NetworkManager> NetworkManagers = new List<NetworkManager>();

        internal static CoroutineRunner CoroutineRunner;

        internal static Queue<QueuedSceneJob> QueuedSceneJobs = new Queue<QueuedSceneJob>();
        internal List<Coroutine> CoroutinesRunning = new List<Coroutine>();
        internal static Coroutine SceneJobProcessor;
        internal static QueuedSceneJob CurrentQueuedSceneJob;
        protected static WaitForSeconds s_WaitForSeconds;


        public delegate bool CanClientsLoadUnloadDelegateHandler();
        public static event CanClientsLoadUnloadDelegateHandler CanClientsLoad;
        public static event CanClientsLoadUnloadDelegateHandler CanClientsUnload;


        public static bool VerboseDebugMode;
        /// <summary>
        /// Used for loading scenes on the client-side during
        /// an integration test
        /// </summary>
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
            public SceneEventProgress SceneEventProgress;
            public IntegrationTestSceneHandler IntegrationTestSceneHandler;
        }

        internal NetworkManager NetworkManager;

        internal string NetworkManagerName;

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


        internal static void VerboseDebug(string message)
        {
            if (VerboseDebugMode)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Processes scene loading jobs
        /// </summary>
        /// <param name="queuedSceneJob">job to process</param>
        static internal IEnumerator ProcessLoadingSceneJob(QueuedSceneJob queuedSceneJob)
        {
            var itegrationTestSceneHandler = queuedSceneJob.IntegrationTestSceneHandler;
            while (!itegrationTestSceneHandler.OnCanClientsLoad())
            {
                yield return s_WaitForSeconds;
            }

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            // We always load additively for all scenes during integration tests
            var asyncOperation = SceneManager.LoadSceneAsync(queuedSceneJob.SceneName, LoadSceneMode.Additive);
            queuedSceneJob.SceneEventProgress.SetAsyncOperation(asyncOperation);

            // Wait for it to finish
            while (queuedSceneJob.JobType != QueuedSceneJob.JobTypes.Completed)
            {
                yield return s_WaitForSeconds;
            }
            yield return s_WaitForSeconds;
        }

        /// <summary>
        /// Handles scene loading and assists with making sure the right NetworkManagerOwner
        /// is assigned to newly instantiated NetworkObjects.
        ///
        /// Note: Static property usage is OK since jobs are processed one at a time
        /// </summary>
        private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (CurrentQueuedSceneJob.JobType != QueuedSceneJob.JobTypes.Completed && CurrentQueuedSceneJob.SceneName == scene.name)
            {
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;

                ProcessInSceneObjects(scene, CurrentQueuedSceneJob.IntegrationTestSceneHandler.NetworkManager);

                CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;
            }
        }

        /// <summary>
        /// Handles some pre-spawn processing of in-scene placed NetworkObjects
        /// to make sure the appropriate NetworkManagerOwner is assigned.  It
        /// also makes sure that each in-scene placed NetworkObject has an
        /// ObjectIdentifier component if one is not assigned to it or its
        /// children.
        /// </summary>
        /// <param name="scene">the scenes that was just loaded</param>
        /// <param name="networkManager">the relative NetworkManager</param>
        private static void ProcessInSceneObjects(Scene scene, NetworkManager networkManager)
        {
            // Get all in-scene placed NeworkObjects that were instantiated when this scene loaded
            var inSceneNetworkObjects = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.IsSceneObject != false && c.GetSceneOriginHandle() == scene.handle);
            foreach (var sobj in inSceneNetworkObjects)
            {
                if (sobj.NetworkManagerOwner != networkManager)
                {
                    sobj.NetworkManagerOwner = networkManager;
                }
                if (sobj.GetComponent<ObjectNameIdentifier>() == null && sobj.GetComponentInChildren<ObjectNameIdentifier>() == null)
                {
                    sobj.gameObject.AddComponent<ObjectNameIdentifier>();
                }
            }
        }

        /// <summary>
        /// Processes scene unloading jobs
        /// </summary>
        /// <param name="queuedSceneJob">job to process</param>
        static internal IEnumerator ProcessUnloadingSceneJob(QueuedSceneJob queuedSceneJob)
        {
            var itegrationTestSceneHandler = queuedSceneJob.IntegrationTestSceneHandler;
            while (!itegrationTestSceneHandler.OnCanClientsUnload())
            {
                yield return s_WaitForSeconds;
            }

            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
            if (queuedSceneJob.Scene.IsValid() && queuedSceneJob.Scene.isLoaded && !queuedSceneJob.Scene.name.Contains(NetcodeIntegrationTestHelpers.FirstPartOfTestRunnerSceneName))
            {
                var asyncOperation = SceneManager.UnloadSceneAsync(queuedSceneJob.Scene);
                queuedSceneJob.SceneEventProgress.SetAsyncOperation(asyncOperation);
            }
            else
            {
                CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;
            }

            // Wait for it to finish
            while (queuedSceneJob.JobType != QueuedSceneJob.JobTypes.Completed)
            {
                yield return s_WaitForSeconds;
            }
        }

        /// <summary>
        /// Handles closing out scene unloading jobs
        /// </summary>
        private static void SceneManager_sceneUnloaded(Scene scene)
        {
            if (CurrentQueuedSceneJob.JobType != QueuedSceneJob.JobTypes.Completed && CurrentQueuedSceneJob.Scene.name == scene.name)
            {
                SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;

                CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;
            }
        }

        /// <summary>
        /// Processes all jobs within the queue.
        /// When all jobs are finished, the coroutine stops.
        /// </summary>
        static internal IEnumerator JobQueueProcessor()
        {
            while (QueuedSceneJobs.Count != 0)
            {
                CurrentQueuedSceneJob = QueuedSceneJobs.Dequeue();
                VerboseDebug($"[ITSH-START] {CurrentQueuedSceneJob.IntegrationTestSceneHandler.NetworkManagerName} processing {CurrentQueuedSceneJob.JobType} for scene {CurrentQueuedSceneJob.SceneName}.");
                if (CurrentQueuedSceneJob.JobType == QueuedSceneJob.JobTypes.Loading)
                {
                    yield return ProcessLoadingSceneJob(CurrentQueuedSceneJob);
                }
                else if (CurrentQueuedSceneJob.JobType == QueuedSceneJob.JobTypes.Unloading)
                {
                    yield return ProcessUnloadingSceneJob(CurrentQueuedSceneJob);
                }
                VerboseDebug($"[ITSH-STOP] {CurrentQueuedSceneJob.IntegrationTestSceneHandler.NetworkManagerName} processing {CurrentQueuedSceneJob.JobType} for scene {CurrentQueuedSceneJob.SceneName}.");
            }
            SceneJobProcessor = null;
            yield break;
        }

        /// <summary>
        /// Adds a job to the job queue, and if  the JobQueueProcessor coroutine
        /// is not running then it will be started as well.
        /// </summary>
        /// <param name="queuedSceneJob">job to add to the queue</param>
        private void AddJobToQueue(QueuedSceneJob queuedSceneJob)
        {
            QueuedSceneJobs.Enqueue(queuedSceneJob);
            if (SceneJobProcessor == null)
            {
                SceneJobProcessor = CoroutineRunner.StartCoroutine(JobQueueProcessor());
            }
        }

        private string m_ServerSceneBeingLoaded;
        /// <summary>
        /// Server always loads like it normally would
        /// </summary>
        public AsyncOperation GenericLoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress)
        {
            m_ServerSceneBeingLoaded = sceneName;
            if (NetcodeIntegrationTest.IsRunning)
            {
                SceneManager.sceneLoaded += Sever_SceneLoaded;
            }
            var operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            sceneEventProgress.SetAsyncOperation(operation);
            return operation;
        }

        private void Sever_SceneLoaded(Scene scene, LoadSceneMode arg1)
        {
            if (m_ServerSceneBeingLoaded == scene.name)
            {
                ProcessInSceneObjects(scene, NetworkManager);
                SceneManager.sceneLoaded -= Sever_SceneLoaded;
            }
        }

        /// <summary>
        /// Server always unloads like it normally would
        /// </summary>
        public AsyncOperation GenericUnloadSceneAsync(Scene scene, SceneEventProgress sceneEventProgress)
        {
            var operation = SceneManager.UnloadSceneAsync(scene);
            sceneEventProgress.SetAsyncOperation(operation);
            return operation;
        }


        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, SceneEventProgress sceneEventProgress)
        {
            // Server and non NetcodeIntegrationTest tests use the generic load scene method
            if (!NetcodeIntegrationTest.IsRunning)
            {
                return GenericLoadSceneAsync(sceneName, loadSceneMode, sceneEventProgress);
            }
            else // NetcodeIntegrationTest Clients always get added to the jobs queue
            {
                AddJobToQueue(new QueuedSceneJob() { IntegrationTestSceneHandler = this, SceneName = sceneName, SceneEventProgress = sceneEventProgress, JobType = QueuedSceneJob.JobTypes.Loading });
            }

            return null;
        }

        public AsyncOperation UnloadSceneAsync(Scene scene, SceneEventProgress sceneEventProgress)
        {
            // Server and non NetcodeIntegrationTest tests use the generic unload scene method
            if (!NetcodeIntegrationTest.IsRunning)
            {
                return GenericUnloadSceneAsync(scene, sceneEventProgress);
            }
            else // NetcodeIntegrationTest Clients always get added to the jobs queue
            {
                AddJobToQueue(new QueuedSceneJob() { IntegrationTestSceneHandler = this, Scene = scene, SceneEventProgress = sceneEventProgress, JobType = QueuedSceneJob.JobTypes.Unloading });
            }
            // This is OK to return a "nothing" AsyncOperation since we are simulating client loading
            return null;
        }

        /// <summary>
        /// Replacement callback takes other NetworkManagers into consideration
        /// </summary>
        internal Scene GetAndAddNewlyLoadedSceneByName(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sceneLoaded = SceneManager.GetSceneAt(i);
                if (sceneLoaded.name == sceneName)
                {
                    var skip = false;
                    foreach (var networkManager in NetworkManagers)
                    {
                        if (NetworkManager.LocalClientId == networkManager.LocalClientId || !networkManager.IsListening)
                        {
                            continue;
                        }
                        if (networkManager.SceneManager.ScenesLoaded.ContainsKey(sceneLoaded.handle))
                        {
                            if (NetworkManager.LogLevel == LogLevel.Developer)
                            {
                                NetworkLog.LogInfo($"{NetworkManager.name}'s ScenesLoaded contains {sceneLoaded.name} with a handle of {sceneLoaded.handle}.  Skipping over scene.");
                            }
                            skip = true;
                            break;
                        }
                    }

                    if (!skip && !NetworkManager.SceneManager.ScenesLoaded.ContainsKey(sceneLoaded.handle))
                    {
                        if (NetworkManager.LogLevel == LogLevel.Developer)
                        {
                            NetworkLog.LogInfo($"{NetworkManager.name} adding {sceneLoaded.name} with a handle of {sceneLoaded.handle} to its ScenesLoaded.");
                        }
                        NetworkManager.SceneManager.ScenesLoaded.Add(sceneLoaded.handle, sceneLoaded);
                        return sceneLoaded;
                    }
                }
            }

            throw new Exception($"Failed to find any loaded scene named {sceneName}!");
        }

        private bool ExcludeSceneFromSynchronizationCheck(Scene scene)
        {
            if (!NetworkManager.SceneManager.ScenesLoaded.ContainsKey(scene.handle) && SceneManager.GetActiveScene().handle != scene.handle)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Constructor now must take NetworkManager
        /// </summary>
        public IntegrationTestSceneHandler(NetworkManager networkManager)
        {
            networkManager.SceneManager.OverrideGetAndAddNewlyLoadedSceneByName = GetAndAddNewlyLoadedSceneByName;
            networkManager.SceneManager.ExcludeSceneFromSychronization = ExcludeSceneFromSynchronizationCheck;
            NetworkManagers.Add(networkManager);
            NetworkManagerName = networkManager.name;
            if (s_WaitForSeconds == null)
            {
                s_WaitForSeconds = new WaitForSeconds(1.0f / networkManager.NetworkConfig.TickRate);
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
                SceneJobProcessor = null;
            }

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
