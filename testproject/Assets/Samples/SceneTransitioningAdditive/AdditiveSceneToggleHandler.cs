using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

using MLAPI;
using MLAPI.SceneManagement;

namespace TestProject.ManualTests
{
    public class AdditiveSceneToggleHandler : NetworkBehaviour
    {
        public static bool ExitingNow { get; internal set; }

        private Toggle m_ToggleObject;

        [HideInInspector]
        [SerializeField]
        private string m_SceneToLoad;

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
        private int m_CurrentSceneIndex;

        private void Awake()
        {
            ExitingNow = false;
        }

        private void Start()
        {
            m_ToggleObject = gameObject.GetComponentInChildren<Toggle>();
            StartCoroutine(CheckForVisibility());

            //NetworkManager.SceneManager.OnSceneSwitchStarted += SceneManager_OnSceneSwitchStarted;
        }

        //private void SceneManager_OnSceneSwitchStarted(AsyncOperation operation)
        //{
        //    if (m_ToggleObject)
        //    {
        //        if (m_ToggleObject.isOn)
        //        {
        //            m_CurrentSceneSwitchProgress = NetworkManager.Singleton.SceneManager.UnloadScene(m_SceneToLoad);
        //        }
        //    }
        //}

        private bool m_ExitingScene;
        private void OnDestroy()
        {
            m_ExitingScene = true;
            StopCoroutine(CheckForVisibility());
        }

        private IEnumerator CheckForVisibility()
        {
            while (!m_ExitingScene)
            {
                if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
                {
                    if (m_ToggleObject)
                    {
                        m_ToggleObject.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (m_ToggleObject)
                    {
                        m_ToggleObject.gameObject.SetActive(false);
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }

            yield return null;
        }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                if (m_ToggleObject)
                {
                    m_ToggleObject.gameObject.SetActive(true);
                }
            }
            else
            {
                if (m_ToggleObject)
                {
                    m_ToggleObject.gameObject.SetActive(false);
                }
            }
            base.OnNetworkSpawn();
        }

        private SceneSwitchProgress m_CurrentSceneSwitchProgress;


        public void OnToggle()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
            {
                if (m_ToggleObject)
                {
                    if(m_ToggleObject.isOn)
                    {
                        m_CurrentSceneSwitchProgress = NetworkManager.Singleton.SceneManager.LoadScene(m_SceneToLoad);
                    }
                    else
                    {
                        m_CurrentSceneSwitchProgress = NetworkManager.Singleton.SceneManager.UnloadScene(m_SceneToLoad);
                    }
                }
            }
        }

        public delegate void OnSceneSwitchCompletedDelegateHandler();

        public event OnSceneSwitchCompletedDelegateHandler OnSceneSwitchCompleted;

        private void CurrentSceneSwitchProgress_OnComplete(bool timedOut)
        {
            OnSceneSwitchCompleted?.Invoke();
        }
    }
}
