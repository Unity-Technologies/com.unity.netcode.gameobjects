using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;


/// <summary>
/// Used in tandem with the ConnectModeButtons prefab asset in test project
/// </summary>
public class ConnectionModeScript : MonoBehaviour
{
    [SerializeField]
    private GameObject m_ConnectionModeButtons;

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
            m_ConnectionModeButtons.SetActive(false);
        }
    }


}
