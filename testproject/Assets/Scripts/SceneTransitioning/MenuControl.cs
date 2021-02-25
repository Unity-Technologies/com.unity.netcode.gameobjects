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
    private string m_LobbySceneName = "GameLobby";

    public void StartLocalGame()
    {
        // Update the current HostNameInput with whatever we have set in the NetworkConfig as default
        var unetTransport = (UnetTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport;
        if (unetTransport) m_HostIpInput.text = unetTransport.ConnectAddress;
       //[NSS-TODO]: Fix how this is handled (See LobbyControl.cs)
        LobbyControl.isHosting = true;
        GlobalGameState.Singleton.SwitchScene(m_LobbySceneName);
    }

    public void JoinLocalGame()
    {
        if (m_HostIpInput.text != "Hostname")
        {
            var unetTransport = (UnetTransport)NetworkingManager.Singleton.NetworkConfig.NetworkTransport;
            if (unetTransport) unetTransport.ConnectAddress = m_HostIpInput.text;
            //[NSS-TODO]: Fix how this is handled (See LobbyControl.cs)
            LobbyControl.isHosting = false;
            GlobalGameState.Singleton.SwitchScene(m_LobbySceneName);
        }
    }
}
