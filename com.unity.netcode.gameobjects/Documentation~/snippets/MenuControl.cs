using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MenuControl : MonoBehaviour
{
    [SerializeField]
    private TMP_Text m_HostIpInput;

    [SerializeField]
    private string m_LobbySceneName = "InvadersLobby";

    public void StartLocalGame()
    {
        // Update the current HostNameInput with whatever we have set in the NetworkConfig as default
        var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport) m_HostIpInput.text = "127.0.0.1";
        if (NetworkManager.Singleton.StartHost())
        {
            SceneTransitionHandler.sceneTransitionHandler.RegisterCallbacks();
            SceneTransitionHandler.sceneTransitionHandler.SwitchScene(m_LobbySceneName);
        }
        else
        {
            Debug.LogError("Failed to start host.");
        }
    }

    public void JoinLocalGame()
    {
        if (m_HostIpInput.text != "Hostname")
        {
            var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            if (utpTransport)
            {
                utpTransport.SetConnectionData(Sanitize(m_HostIpInput.text), 7777);
            }
            if (!NetworkManager.Singleton.StartClient())
            {
                Debug.LogError("Failed to start client.");
            }
        }
    }
    
    public static string Sanitize(string dirtyString)
    {
        // sanitize the input for the ip address
        return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
    }
}
