using UnityEngine;
using MLAPI;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UIController : MonoBehaviour
{
    public NetworkManager NetworkManager;
    public GameObject ButtonsRoot;

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
    }
}
