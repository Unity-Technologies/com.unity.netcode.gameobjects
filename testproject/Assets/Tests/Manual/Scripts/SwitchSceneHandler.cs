using System.Collections;
using UnityEngine;
using MLAPI;
using MLAPI.SceneManagement;



public class SwitchSceneHandler : NetworkBehaviour
{
    public static bool ExitingNow { get; internal set; }

    [SerializeField]
    private GameObject m_SwitchSceneButtonObject;

    [SerializeField]
    private string m_SceneToSwitchTo;

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
        while(!m_ExitingScene)
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
            m_CurrentSceneSwitchProgress = NetworkManager.Singleton.SceneManager.SwitchScene(m_SceneToSwitchTo);

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

