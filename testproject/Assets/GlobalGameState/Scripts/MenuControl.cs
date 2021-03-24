using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Configuration;
using MLAPI.Transports.UNET;

public class MenuControl : MonoBehaviour
{
    [SerializeField]
    private InputField m_HostIpInput = null;

    private UNetTransport m_CurrentTransport;

    private void Start()
    {
        if (NetworkManager.Singleton)
        {
            var NetConfig = NetworkManager.Singleton.NetworkConfig;
            if (NetConfig != null)
            {
                //Update the host input IP Address
                m_CurrentTransport = (UNetTransport)NetConfig.NetworkTransport;
                if (m_CurrentTransport)
                {
                    m_HostIpInput.text = m_CurrentTransport.ConnectAddress;
                }
            }
        }
#if UNITY_EDITOR
        else
        {
            //EDITOR ONLY!!
            //This will automatically launch the MLAPIBootStrap and then transition directly to the scene this control is contained within (for easy development of scenes)
            GlobalGameState.LoadBootStrapScene();
        }
#endif
    }

    public void StartLocalGame()
    {
        GlobalGameState.Singleton.IsHostingGame = true;
        m_CurrentTransport.ConnectAddress = m_HostIpInput.text;
        GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.Lobby);
    }

    public void JoinLocalGame()
    {
        GlobalGameState.Singleton.IsHostingGame = false;
        m_CurrentTransport.ConnectAddress = m_HostIpInput.text;
        GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.Lobby);
    }
}
