using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class SwitchSceneHandler : NetworkBehaviour
    {
        public static bool ExitingNow { get; internal set; }

        [SerializeField]
        private GameObject m_SwitchSceneButtonObject;

        [SerializeField]
        private string m_SceneToSwitchTo;

        [Tooltip("If enabled, this will automatically switch the scene after the Auto Switch TimeOut period has elapsed.")]
        [SerializeField]
        private bool m_EnableAutoSwitch;

        [Tooltip("Period in seconds until it will automatically switch to the next scene.")]
        [SerializeField]
        private float m_AutoSwitchTimeOut = 60;

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
                if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
                {
                    if (m_SwitchSceneButtonObject)
                    {
                        m_SwitchSceneButtonObject.SetActive(NetworkManager.Singleton.IsServer);
                    }

                    if (m_EnableAutoSwitch && NetworkManager.Singleton.IsServer)
                    {
                        StartCoroutine(AutoSwitch());
                    }

                    yield return null;
                }
                yield return new WaitForSeconds(0.5f);
            }

            yield return null;
        }

        public override void OnNetworkSpawn()
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
            base.OnNetworkSpawn();
        }

        private IEnumerator AutoSwitch()
        {
            yield return new WaitForSeconds(m_AutoSwitchTimeOut);

            OnSwitchScene();

        }

        public void OnSwitchScene()
        {
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
            {
                m_ExitingScene = true;
                ExitingNow = true;
                var sceneEventProgressStatus = NetworkManager.Singleton.SceneManager.LoadScene(m_SceneToSwitchTo, UnityEngine.SceneManagement.LoadSceneMode.Single);
                if (sceneEventProgressStatus != SceneEventProgressStatus.Started)
                {
                    Debug.LogError($"{nameof(NetworkSceneManager.LoadScene)} returned a {nameof(SceneEventProgressStatus)} value of {sceneEventProgressStatus}");
                }
            }
        }
    }
}
