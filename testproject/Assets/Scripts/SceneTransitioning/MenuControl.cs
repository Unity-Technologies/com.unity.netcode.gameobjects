using System;
using MLAPI;
using MLAPI.SceneManagement;
using MLAPI.Transports.UNET;
using UnityEngine;
using UnityEngine.UI;

public class MenuControl : MonoBehaviour
{
    [SerializeField]
    private Text m_HostIpInput = null;

    [SerializeField]
    private string m_LobbySceneName = "InvadersLobby";

    void Start()
    {

    }

    public void StartLocalGame()
    {

        // Update the current HostNameInput with whatever we have set in the NetworkConfig as default
        var unetTransport = (UnetTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport;
        if (unetTransport) m_HostIpInput.text = unetTransport.ConnectAddress;
        LobbyControl.isHosting = true;  //This is a work around to handle proper instantiation of a scene for the first time.(See LobbyControl.cs)
        GlobalGameState.Singleton.SwitchScene(m_LobbySceneName);
    }

    public void JoinLocalGame()
    {
        if (m_HostIpInput.text != "Hostname")
        {
            var unetTransport = (UnetTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport;
            if (unetTransport) unetTransport.ConnectAddress = m_HostIpInput.text;
            LobbyControl.isHosting = false;//This is a work around to handle proper instantiation of a scene for the first time.  (See LobbyControl.cs)
            GlobalGameState.Singleton.SwitchScene(m_LobbySceneName);
        }
    }
}
