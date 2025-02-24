using UnityEngine;
using System;
using System.Collections;
using UnityEditor;

namespace Unity.Netcode
{
    public class MonobehaviourHelper : MonoBehaviour
    {
        public static event Action OnLateUpdate;
        public static event Action OnUpdate;
        public static event Action OnEndOfFrame;
        public static event Action OnOneSecondPassed;
        private float m_OneSecondTimer = 0f;

        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }

        private void Update()
        {
            OnUpdate?.Invoke();

            // Handle one second timer
            m_OneSecondTimer += Time.deltaTime;
            if (m_OneSecondTimer >= 1f)
            {
                OnOneSecondPassed?.Invoke();
                m_OneSecondTimer = 0f;
            }
        }

        private IEnumerator EndOfFrameCoroutine()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                OnEndOfFrame?.Invoke();
            }
        }


        private static MonobehaviourHelper s_MonobehaviourHelperInstance;

        public static void InitMonoBehaviourHelper()
        {
#if UNITY_EDITOR
            // Handling Edit Time behavior:
            void OnEditorApplicationOnplayModeStateChanged(PlayModeStateChange change)
            {
                Debug.Log("Instantiate MonoBehaviourHelper");

                switch (change)
                {
                    case PlayModeStateChange.EnteredPlayMode:
                    {
                        CreateInstance();
                        break;
                    }
                    case PlayModeStateChange.ExitingPlayMode:
                    {
                        UnityEngine.Object.Destroy(s_MonobehaviourHelperInstance);
                        break;
                    }
                }
            }

            EditorApplication.playModeStateChanged += OnEditorApplicationOnplayModeStateChanged;

            if (EditorApplication.isPlaying)
            {
                CreateInstance();
            }
#else
            CreateInstance();
#endif
        }

        public static void DeinitMonoBehaviourHelper()
        {
            s_MonobehaviourHelperInstance.StopAllCoroutines();
        }

        private static void CreateInstance()
        {
            Debug.Assert(s_MonobehaviourHelperInstance == null, s_MonobehaviourHelperInstance);

            s_MonobehaviourHelperInstance = new GameObject($"[{nameof(MonobehaviourHelper)}]").AddComponent<MonobehaviourHelper>();
            s_MonobehaviourHelperInstance.gameObject.hideFlags = HideFlags.HideAndDontSave;

            // Start the end of frame coroutine
            s_MonobehaviourHelperInstance.StartCoroutine(s_MonobehaviourHelperInstance.EndOfFrameCoroutine());
            UnityEngine.Object.DontDestroyOnLoad(s_MonobehaviourHelperInstance.gameObject);
        }
}}
