using UnityEngine;
using Unity.Netcode;

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
}
