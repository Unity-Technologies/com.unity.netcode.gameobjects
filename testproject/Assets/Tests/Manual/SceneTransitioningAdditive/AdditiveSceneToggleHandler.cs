using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class AdditiveSceneToggleHandler : NetworkBehaviour
    {
        [SerializeField]
        private bool m_ActivateOnLoad = false;

        private Toggle m_ToggleObject;

        [HideInInspector]
        [SerializeField]
        private string m_SceneToLoad;

        private Scene m_SceneLoaded;

#if UNITY_EDITOR
        [SerializeField]
        private SceneAsset m_SceneAsset;
        private void OnValidate()
        {
            if (m_SceneAsset != null && m_SceneAsset.name != m_SceneToLoad)
            {
                m_SceneToLoad = m_SceneAsset.name;
            }
        }
#endif

        private void Start()
        {
            m_ToggleObject = gameObject.GetComponentInChildren<Toggle>();
            StartCoroutine(CheckForVisibility());
        }

        private bool m_ExitingScene;

        public override void OnDestroy()
        {
            m_ExitingScene = true;
            StopCoroutine(CheckForVisibility());

            base.OnDestroy();
        }

        private IEnumerator CheckForVisibility()
        {
            while (!m_ExitingScene)
            {
                if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
                {
                    if (m_ToggleObject)
                    {
                        if (NetworkManager.Singleton.IsServer)
                        {
                            m_ToggleObject.gameObject.SetActive(true);
                            if (m_ActivateOnLoad)
                            {
                                StartCoroutine(DelayedActivate());
                            }
                        }
                        else
                        {
                            m_ToggleObject.gameObject.SetActive(false);
                        }
                    }
                    break;
                }
                else
                {
                    if (m_ToggleObject && m_ToggleObject.gameObject.activeInHierarchy)
                    {
                        m_ToggleObject.gameObject.SetActive(false);
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }

            yield return null;
        }

        private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
                {
                    if (sceneEvent.ClientId == NetworkManager.ServerClientId && !m_SceneLoaded.IsValid()
                        && sceneEvent.Scene.IsValid() && sceneEvent.Scene.name == m_SceneToLoad)
                    {
                        m_SceneLoaded = sceneEvent.Scene;
                        m_WaitForSceneLoadOrUnload = false;
                    }
                }
                else if (sceneEvent.SceneEventType == SceneEventType.UnloadComplete)
                {
                    if (sceneEvent.ClientId == NetworkManager.ServerClientId && !m_SceneLoaded.isLoaded)
                    {
                        m_SceneLoaded = new Scene();
                        m_WaitForSceneLoadOrUnload = false;
                    }
                }
            }
        }

        private IEnumerator DelayedActivate()
        {
            yield return new WaitForSeconds(0.5f);
            if (m_ToggleObject)
            {
                m_ToggleObject.isOn = true;
            }
            yield return null;
        }

        public void OnToggle()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                if (m_ToggleObject)
                {
                    m_ToggleObject.enabled = false;
                    ToggleSceneManager.AddNewToggleHandler(this);
                }
            }
        }


        private bool m_WaitForSceneLoadOrUnload;

        public IEnumerator SceneEventCoroutine()
        {
            var isLoading = m_ToggleObject.isOn;
            var sceneEventProgressStatus = SceneEventProgressStatus.None;
            var continueCheck = true;

            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
            while (continueCheck && sceneEventProgressStatus != SceneEventProgressStatus.Started && sceneEventProgressStatus != SceneEventProgressStatus.SceneFailedVerification)
            {
                if (isLoading)
                {
                    sceneEventProgressStatus = NetworkManager.Singleton.SceneManager.LoadScene(m_SceneToLoad, LoadSceneMode.Additive);
                }
                else
                {
                    sceneEventProgressStatus = NetworkManager.Singleton.SceneManager.UnloadScene(m_SceneLoaded);
                }

                switch (sceneEventProgressStatus)
                {
                    case SceneEventProgressStatus.SceneEventInProgress:
                        {
                            yield return new WaitForSeconds(0.25f);
                            break;
                        }
                    case SceneEventProgressStatus.Started:
                        {
                            continueCheck = false;
                            break;
                        }
                    case SceneEventProgressStatus.InternalNetcodeError:
                    case SceneEventProgressStatus.InvalidSceneName:
                    case SceneEventProgressStatus.SceneNotLoaded:
                        {
                            Debug.Log($"Scene Event Error: {sceneEventProgressStatus}");
                            continueCheck = false;
                            break;
                        }
                }
            }
            m_WaitForSceneLoadOrUnload = true;
            var timeOutAfter = Time.realtimeSinceStartup + 5.0f;
            while (m_WaitForSceneLoadOrUnload)
            {
                if (timeOutAfter < Time.realtimeSinceStartup)
                {
                    Debug.LogWarning("Timed out waiting for scene to load or unload!");
                    m_WaitForSceneLoadOrUnload = false;
                }
                yield return new WaitForSeconds(0.5f);
            }

            NetworkManager.Singleton.SceneManager.OnSceneEvent -= SceneManager_OnSceneEvent;
            m_ToggleObject.isOn = isLoading;
            m_ToggleObject.enabled = true;
            ToggleSceneManager.CurrentQueueItem = null;
            yield return null;
        }
    }


    public static class ToggleSceneManager
    {

        private static Queue<AdditiveSceneToggleHandler> s_QueueUpForLoadUnload = new Queue<AdditiveSceneToggleHandler>();
        public static AdditiveSceneToggleHandler CurrentQueueItem;
        private static IEnumerator GlobalQueueToggleRoutine()
        {
            while (s_QueueUpForLoadUnload.Count > 0)
            {
                CurrentQueueItem = s_QueueUpForLoadUnload.Dequeue();
                CurrentQueueItem.StartCoroutine(CurrentQueueItem.SceneEventCoroutine());
                while (CurrentQueueItem != null)
                {
                    yield return new WaitForSeconds(0.25f);
                }
            }
            yield return null;
        }


        public static void AddNewToggleHandler(AdditiveSceneToggleHandler handler)
        {
            if (s_QueueUpForLoadUnload.Count == 0 && CurrentQueueItem == null)
            {
                s_QueueUpForLoadUnload.Enqueue(handler);
                NetworkManager.Singleton.StartCoroutine(GlobalQueueToggleRoutine());
            }
            else
            {
                s_QueueUpForLoadUnload.Enqueue(handler);
            }
        }
    }
}
