using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneIntermission : NetworkBehaviour
{
    public Text SceneLoadingProgress;
    public Button ActivateSceneButton;
    public SceneLoader SceneLoader;

    public bool IntermissionIsActive { get; private set; }
    public event Action<bool> OnIntermissionActiveUpdate;
    private const float k_ByteRatio = 1.0f / 255.0f;
    private NetworkVariable<byte> m_LoadingProgress = new NetworkVariable<byte>();
    private string m_SceneName;

    private void Awake()
    {
        // Move this to the DDOL so it persists throughout the application lifetime.
        DontDestroyOnLoad(this);
    }

    public void BeginLoadingScene(string sceneName)
    {
        if (!IsSpawned || !IsServer)
        {
            return;
        }
        m_SceneName = sceneName;
        NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
        NetworkManager.SceneManager.LoadScene(m_SceneName, LoadSceneMode.Single);
        SceneLoadingProgress.gameObject.SetActive(true);
        UpdateProgresss();
        LoadStartedRpc();
        IntermissionIsActive = true;
        OnIntermissionActiveUpdate?.Invoke(true);
    }

    private void UpdateProgresss()
    {
        var percentage = Mathf.RoundToInt(m_LoadingProgress.Value * k_ByteRatio * 100.0f);
        SceneLoadingProgress.text = $"Loading: {percentage}%";
    }

    [Rpc(SendTo.NotMe)]
    private void LoadStartedRpc()
    {
        m_LoadingProgress.OnValueChanged += OnLoadingProgressChanged;
        SceneLoadingProgress.gameObject.SetActive(true);
        OnIntermissionActiveUpdate?.Invoke(true);
    }

    [Rpc(SendTo.NotMe)]
    private void LoadEndedRpc()
    {
        m_LoadingProgress.OnValueChanged -= OnLoadingProgressChanged;
        EndIntermission();
    }

    private void OnLoadingProgressChanged(byte previous, byte current)
    {
        UpdateProgresss();
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.ClientId == NetworkManager.LocalClientId && sceneEvent.SceneEventType == SceneEventType.Load)
        {
            NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
            NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            if (SceneLoader)
            {
                sceneEvent.AsyncOperation.allowSceneActivation = !SceneLoader.ShouldDelayFinalSceneLoad(sceneEvent.SceneName);
            }
            else
            {
                sceneEvent.AsyncOperation.allowSceneActivation = true;
            }

            if (!sceneEvent.AsyncOperation.allowSceneActivation)
            {
                m_ShouldActivateScene = false;
                m_LoadingProgress.Value = 0;
                StartCoroutine(DelaySceneActivation(sceneEvent));
            }
        }
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        if (m_SceneName == sceneName)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    public void EndIntermission()
    {
        if (HasAuthority)
        {
            LoadEndedRpc();
        }
        IntermissionIsActive = false;
        m_ShouldActivateScene = false;
        SceneLoadingProgress.gameObject.SetActive(false);
        ActivateSceneButton.gameObject.SetActive(false);
        OnIntermissionActiveUpdate?.Invoke(false);
    }

    private bool m_ShouldActivateScene;
    public void OnActivateSceneClicked()
    {
        m_ShouldActivateScene = true;
    }

    private IEnumerator DelaySceneActivation(SceneEvent sceneEvent)
    {
        var isWaiting = false;
        var waitPeriod = new WaitForSeconds(0.03333f);
        while (!sceneEvent.AsyncOperation.isDone)
        {
            if (sceneEvent.AsyncOperation.progress >= 0.9f)
            {
                if (!isWaiting)
                {
                    ActivateSceneButton.gameObject.SetActive(true);
                    isWaiting = true;
                }
                if (m_ShouldActivateScene)
                {
                    sceneEvent.AsyncOperation.allowSceneActivation = true;

                }
            }
            m_LoadingProgress.Value = (byte)((sceneEvent.AsyncOperation.progress * 255) + (0.10f * 255));
            UpdateProgresss();
            yield return waitPeriod;
        }
    }
}
