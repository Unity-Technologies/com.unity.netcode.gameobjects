using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Configuration;
using MLAPI.Transports.UNET;


public class MenuControl : MonoBehaviour
{
    [SerializeField]
    private InputField m_HostIpInput = null;

    [SerializeField]
    private string m_LobbySceneName = "GameLobby";

    private void Start()
    {

        NetworkingManager NM = NetworkingManager.Singleton;
        if(NM != null)
        {
            NetworkConfig NetConfig = NetworkingManager.Singleton.NetworkConfig;
            if(NetConfig != null)
            {
                //Update the host input IP Address
                var unetTransport = (UnetTransport)NetConfig.NetworkTransport;
                if (unetTransport)
                {
                    m_HostIpInput.text = unetTransport.ConnectAddress;
                }
            }
        }
#if (UNITY_EDITOR)

        else
        {
            GlobalGameState.LoadBootStrapScene();
        }
#endif
    }

    public void StartLocalGame()
    {

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
