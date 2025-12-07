using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButtonScript : MonoBehaviour
{
    [SerializeField]
    private MenuReference m_SceneMenuToLoad;

    [SerializeField]
    private bool m_DestroyNetworkManagerOnExit = true;

    private void Start()
    {
        if (!NetworkManager.Singleton)
        {
            return;
        }

        NetworkManager.Singleton.OnServerStopped += OnNetworkManagerStopped;
        NetworkManager.Singleton.OnClientStopped += OnNetworkManagerStopped;

        NetworkManager.Singleton.OnServerStarted += OnNetworkManagerStarted;
        NetworkManager.Singleton.OnClientStarted += OnNetworkManagerStarted;
    }

    private void OnNetworkManagerStarted()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        var disconnectedReason = "Disconnected from session.";
        if (NetworkManager.Singleton && !string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
        {
            disconnectedReason = $"{NetworkManager.Singleton.DisconnectReason}";
        }
        Debug.Log($"[Client-{clientId}] {disconnectedReason}");
    }

    /// <summary>
    /// A very basic way to exit back to the first scene in the build settings
    /// </summary>
    public void OnExitScene()
    {
        if (!NetworkManager.Singleton)
        {
            LoadExitScene();
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            if (!NetworkManager.Singleton.ShutdownInProgress)
            {

                NetworkManager.Singleton.Shutdown();
            }
        }
        else
        {
            LoadExitScene();
        }
    }

    private void LoadExitScene()
    {
        if (m_DestroyNetworkManagerOnExit)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if (m_SceneMenuToLoad != null && m_SceneMenuToLoad.GetReferencedScenes()[0] != string.Empty)
        {
            SceneManager.LoadSceneAsync(m_SceneMenuToLoad.GetReferencedScenes()[0], LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
        }
    }

    private void OnNetworkManagerStopped(bool obj)
    {
        NetworkManager.Singleton.OnServerStopped -= OnNetworkManagerStopped;
        NetworkManager.Singleton.OnClientStopped -= OnNetworkManagerStopped;
        LoadExitScene();
    }
}
