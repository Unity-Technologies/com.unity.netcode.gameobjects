using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Used in tandem with the ConnectModeButtons prefab asset in test project
/// </summary>
public class ConnectionModeScript : MonoBehaviour
{
    [SerializeField]
    private GameObject m_ConnectionModeButtons;

    private CommandLineProcessor m_CommandLineProcessor;
    internal void SetCommandLineHandler(CommandLineProcessor commandLineProcessor)
    {
        m_CommandLineProcessor = commandLineProcessor;
        if (m_CommandLineProcessor.AutoConnectEnabled())
        {
            StartCoroutine(WaitForNetworkManager());
        }
    }

    public delegate void OnNotifyConnectionEventDelegateHandler();

    public event OnNotifyConnectionEventDelegateHandler OnNotifyConnectionEventServer;
    public event OnNotifyConnectionEventDelegateHandler OnNotifyConnectionEventHost;
    public event OnNotifyConnectionEventDelegateHandler OnNotifyConnectionEventClient;


    private IEnumerator WaitForNetworkManager()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            try
            {
                if (NetworkManager.Singleton && !NetworkManager.Singleton.IsListening)
                {
                    m_ConnectionModeButtons.SetActive(false);
                    m_CommandLineProcessor.ProcessCommandLine();
                    break;
                }
            }
            catch { }
        }
        yield return null;
    }

    // Start is called before the first frame update
    private void Start()
    {
        //If we have a NetworkManager instance and we are not listening and m_ConnectionModeButtons is not null then show the connection mode buttons
        if (m_ConnectionModeButtons)
        {
            m_ConnectionModeButtons.SetActive(NetworkManager.Singleton && !NetworkManager.Singleton.IsListening);
        }
    }

    /// <summary>
    /// Handles starting MLAPI in server mode
    /// </summary>
    public void OnStartServer()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsListening && m_ConnectionModeButtons)
        {
            NetworkManager.Singleton.StartServer();
            OnNotifyConnectionEventServer?.Invoke();
            m_ConnectionModeButtons.SetActive(false);

        }
    }

    /// <summary>
    /// Handles starting MLAPI in host mode
    /// </summary>
    public void OnStartHost()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsListening && m_ConnectionModeButtons)
        {
            NetworkManager.Singleton.StartHost();
            OnNotifyConnectionEventHost?.Invoke();
            m_ConnectionModeButtons.SetActive(false);
        }
    }

    /// <summary>
    /// Handles starting MLAPI in client mode
    /// </summary>
    public void OnStartClient()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsListening && m_ConnectionModeButtons)
        {
            NetworkManager.Singleton.StartClient();
            OnNotifyConnectionEventClient?.Invoke();
            m_ConnectionModeButtons.SetActive(false);
        }
    }
}
