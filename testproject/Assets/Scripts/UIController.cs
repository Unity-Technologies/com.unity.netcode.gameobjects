using UnityEngine;
using MLAPI;

public class UIController : MonoBehaviour
{
    public NetworkManager NetworkManager;
    public GameObject ButtonsUI;

    public void CreateServer()
    {
        NetworkManager.StartServer();
        HideButtons();
    }

    public void CreateHost()
    {
        NetworkManager.StartHost();
        HideButtons();
    }

    public void JoinGame()
    {
        NetworkManager.StartClient();
        HideButtons();
    }

    private void HideButtons()
    {
        ButtonsUI.SetActive(false);
    }
}
