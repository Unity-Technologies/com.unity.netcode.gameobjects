using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    /// <summary>
    /// This provides basic scene loading and unloading
    /// functionality for smoke tests.
    /// </summary>
    public class SceneAwareSmokeTestState : SmokeTestState
    {
        public string SceneBeingProcessed { get; internal set; }
        public bool SceneIsProcessed { get; internal set; }


        protected string GetSceneNameFromPath(string scenePath)
        {
            var begin = scenePath.LastIndexOf("/", System.StringComparison.Ordinal) + 1;
            var end = scenePath.LastIndexOf(".", System.StringComparison.Ordinal);
            return scenePath.Substring(begin, end - begin);
        }

        protected List<string> GetSceneNamesFromBuildSettings()
        {
            var sceneNamesInBuildSettings = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                sceneNamesInBuildSettings.Add(GetSceneNameFromPath(SceneUtility.GetScenePathByBuildIndex(i)));
            }
            return sceneNamesInBuildSettings;
        }

        protected bool StartLoadingScene(string sceneName)
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneBeingProcessed = sceneName;
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (asyncOp == null)
            {
                return false;
            }
            SceneIsProcessed = false;
            return true;
        }

        public virtual bool OnSceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            return true;
        }

        private void SceneManager_sceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            // Always check SceneIsProcessed last!
            // Order of operations rule: If either SceneIsProcessed is set to true or the
            // OnSceneLoaded method returns true, then we no longer need to be subscribed
            // to the sceneLoaded event.
            var sceneProcessed = OnSceneLoaded(sceneLoaded, loadMode) | SceneIsProcessed;
            if (sceneProcessed)
            {
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            }
        }

        protected bool StartUnloadingScene(Scene sceneToUnload)
        {
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
            SceneBeingProcessed = sceneToUnload.name;
            var asyncOp = SceneManager.UnloadSceneAsync(sceneToUnload);
            if (asyncOp == null)
            {
                return false;
            }

            SceneIsProcessed = false;
            return true;
        }

        protected virtual bool OnSceneUnloaded(Scene sceneUnloaded)
        {
            return true;
        }

        private void SceneManager_sceneUnloaded(Scene sceneUnloaded)
        {
            var sceneProcessed = OnSceneUnloaded(sceneUnloaded) | SceneIsProcessed;
            if (sceneProcessed)
            {
                SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
            }
        }
    }
}
