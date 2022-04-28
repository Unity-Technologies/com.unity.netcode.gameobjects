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

        // Default client simulated delay time
        protected const float k_ClientLoadingSimulatedDelay = 0.016f;

        // Controls the client simulated delay time
        protected static float s_ClientLoadingSimulatedDelay = k_ClientLoadingSimulatedDelay;


        internal static Queue<QueuedSceneJob> QueuedSceneJobs = new Queue<QueuedSceneJob>();
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
            SceneManager.LoadSceneAsync(queuedSceneJob.SceneName, LoadSceneMode.Additive);

            // Wait for it to finish
            while (queuedSceneJob.JobType != QueuedSceneJob.JobTypes.Completed)
            {
                yield return s_WaitForSeconds;
            }
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

                CurrentQueuedSceneJob.SceneAction.Invoke();
                CurrentQueuedSceneJob.JobType = QueuedSceneJob.JobTypes.Completed;
            }
        }


        private static void ProcessInSceneObjects(Scene scene, NetworkManager networkManager)
        {
            // Get all in-scene placed NeworkObjects that were instantiated when this scene loaded
            var inSceneNetworkObjects = Object.FindObjectsOfType<NetworkObject>().Where((c) => c.IsSceneObject != false && c.gameObject.scene.handle == scene.handle);
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
            if (queuedSceneJob.Scene.IsValid() && queuedSceneJob.Scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(queuedSceneJob.Scene);
            }
            else
            {
                CurrentQueuedSceneJob.SceneAction.Invoke();
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
                CurrentQueuedSceneJob.SceneAction.Invoke();
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
                if (CurrentQueuedSceneJob.JobType == QueuedSceneJob.JobTypes.Loading)
                {
                    yield return ProcessLoadingSceneJob(CurrentQueuedSceneJob);
                }
                else if (CurrentQueuedSceneJob.JobType == QueuedSceneJob.JobTypes.Unloading)
                {
                    yield return ProcessUnloadingSceneJob(CurrentQueuedSceneJob);
                }
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
        public AsyncOperation GenericLoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            m_ServerSceneBeingLoaded = sceneName;
            if (NetcodeIntegrationTest.IsRunning)
            {
                SceneManager.sceneLoaded += Sever_SceneLoaded;
            }
            var operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

            operation.completed += new Action<AsyncOperation>(asyncOp2 => { sceneEventAction.Invoke(); });
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
        public AsyncOperation GenericUnloadSceneAsync(Scene scene, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            var operation = SceneManager.UnloadSceneAsync(scene);
            operation.completed += new Action<AsyncOperation>(asyncOp2 => { sceneEventAction.Invoke(); });
            return operation;
        }


        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            if (NetworkManager.IsServer || !NetcodeIntegrationTest.IsRunning)
            {
                return GenericLoadSceneAsync(sceneName, loadSceneMode, sceneEventAction);
            }
            else // Clients are always processed in the queue
            {
                AddJobToQueue(new QueuedSceneJob() { IntegrationTestSceneHandler = this, SceneName = sceneName, SceneAction = sceneEventAction, JobType = QueuedSceneJob.JobTypes.Loading });
            }
            // This is OK to return a "nothing" AsyncOperation since we are simulating client loading
            return new AsyncOperation();
        }

        public AsyncOperation UnloadSceneAsync(Scene scene, ISceneManagerHandler.SceneEventAction sceneEventAction)
        {
            if (NetworkManager.IsServer || !NetcodeIntegrationTest.IsRunning)
            {
                return GenericUnloadSceneAsync(scene, sceneEventAction);
            }
            else // Clients are always processed in the queue
            {
                AddJobToQueue(new QueuedSceneJob() { IntegrationTestSceneHandler = this, Scene = scene, SceneAction = sceneEventAction, JobType = QueuedSceneJob.JobTypes.Unloading });
            }
            // This is OK to return a "nothing" AsyncOperation since we are simulating client loading
            return new AsyncOperation();
        }

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
                        if (NetworkManager.LocalClientId == networkManager.LocalClientId)
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

        /// <summary>
        /// Constructor now must take NetworkManager
        /// </summary>
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
