using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.SceneManagement;
using MLAPI.TestAssets.MenuManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MLAPI.TestAssets.SceneManagement
{
    [AddComponentMenu("MLAPI/TestAssets/SceneManagement/SwitchSceneHandler")]
    public class SwitchSceneHandler : NetworkBehaviour
    {
        public static bool ExitingNow { get; internal set; }

        [SerializeField]
        private GameObject m_SwitchSceneButtonObject;

        [SerializeField]
        internal  SceneReference SceneToSwitchTo;

        [HideInInspector]
        [SerializeField]
        internal string SceneNameToSwitchTo;

        private void Awake()
        {
            
            ExitingNow = false;
        }

        private void Start()
        {            
            m_SwitchSceneButtonObject.SetActive(false);
            StartCoroutine(CheckForVisibility());
        }

        private bool m_ExitingScene;
        private void OnDestroy()
        {
            m_ExitingScene = true;
            StopAllCoroutines();
        }

        private IEnumerator CheckForVisibility()
        {
            while (!m_ExitingScene)
            {
                if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
                {
                    if (m_SwitchSceneButtonObject)
                    {
                        m_SwitchSceneButtonObject.SetActive(true);
                    }
                }
                else
                {
                    m_SwitchSceneButtonObject.SetActive(false);
                }

                yield return new WaitForSeconds(0.5f);
            }

            yield return null;
        }

        public override void NetworkStart()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
            {
                if (m_SwitchSceneButtonObject)
                {
                    m_SwitchSceneButtonObject.SetActive(true);
                }
            }
            else
            {
                m_SwitchSceneButtonObject.SetActive(false);
            }
            base.NetworkStart();
        }

        private SceneSwitchProgress m_CurrentSceneSwitchProgress;

        public delegate void OnSceneSwitchBeginDelegateHandler();

        public event OnSceneSwitchBeginDelegateHandler OnSceneSwitchBegin;

        public void OnSwitchScene()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
            {
                OnSceneSwitchBegin?.Invoke();
                m_ExitingScene = true;
                ExitingNow = true;
                m_CurrentSceneSwitchProgress = NetworkManager.Singleton.SceneManager.SwitchScene(SceneNameToSwitchTo);

                m_CurrentSceneSwitchProgress.OnComplete += CurrentSceneSwitchProgress_OnComplete;
            }
        }

        public delegate void OnSceneSwitchCompletedDelegateHandler();

        public event OnSceneSwitchCompletedDelegateHandler OnSceneSwitchCompleted;

        private void CurrentSceneSwitchProgress_OnComplete(bool timedOut)
        {
            OnSceneSwitchCompleted?.Invoke();
        }
    }


#if UNITY_EDITOR

    // attach this custom inspector to all MySpecialComponent components
    [CustomEditor(typeof(SwitchSceneHandler))]
    public class SceneReferenceComponentInspector : Editor
    {
        private SwitchSceneHandler m_SwitchSceneHandler;

        private void OnEnable()
        {
            m_SwitchSceneHandler = (SwitchSceneHandler)target;
        }

        private void OnValidate()
        {
            if(m_SwitchSceneHandler != null && m_SwitchSceneHandler.SceneNameToSwitchTo == string.Empty && m_SwitchSceneHandler.SceneToSwitchTo != null)
            {
                m_SwitchSceneHandler.SceneNameToSwitchTo = m_SwitchSceneHandler.SceneToSwitchTo.GetFirstSceneName();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Rect rectangle = EditorGUILayout.BeginHorizontal();
            if (m_SwitchSceneHandler != null && m_SwitchSceneHandler.SceneToSwitchTo != null)
            {
                var index = 0;
                var selectionOptions = new List<string>(m_SwitchSceneHandler.SceneToSwitchTo.GetReferencedScenes());
                if (m_SwitchSceneHandler.SceneNameToSwitchTo != string.Empty)
                {
                    if (selectionOptions.Contains(m_SwitchSceneHandler.SceneNameToSwitchTo))
                    {
                        index = selectionOptions.IndexOf(m_SwitchSceneHandler.SceneNameToSwitchTo);
                    }
                }
                var selectedIndex = EditorGUILayout.Popup("Selected Scene:", index, selectionOptions.ToArray(), EditorStyles.popup);
                if (selectedIndex != index)
                {
                    m_SwitchSceneHandler.SceneNameToSwitchTo = selectionOptions[selectedIndex];
                }


            }
            EditorGUILayout.EndHorizontal();

        }
    }
#endif

}
