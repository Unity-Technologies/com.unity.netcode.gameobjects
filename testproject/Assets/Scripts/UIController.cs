using UnityEngine;
using MLAPI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using MLAPI.Transports;

public class UIController : MonoBehaviour
{
    public NetworkManager NetworkManager;
    public GameObject ButtonsRoot;
    public GameObject AuthButton;
    public GameObject JoinCode;

    public UTPTransport Transport;

    private void Awake()
    {
        if (Transport.Protocol == UTPTransport.ProtocolType.RelayUnityTransport) {
            HideButtons();
            JoinCode.SetActive(false);
        }
    }

    public void StartServer()
    {
        NetworkManager.StartServer();
        HideButtons();
    }

    public void StartHost()
    {
        NetworkManager.StartHost();
        HideButtons();
    }

    public void StartClient()
    {
        NetworkManager.StartClient();
        HideButtons();
    }

    private void HideButtons()
    {
        ButtonsRoot.SetActive(false);
    }

    public async void OnSignIn()
    {
        await UnityServices.Initialize();
        Debug.Log("OnSignIn");
        await Authentication.SignInAnonymously();
        Debug.Log($"Logging in with PlayerID {Authentication.PlayerId}");

        if (Authentication.IsSignedIn) {
            ButtonsRoot.SetActive(true);
            JoinCode.SetActive(true);
            AuthButton.SetActive(false);
        }
    }
}
