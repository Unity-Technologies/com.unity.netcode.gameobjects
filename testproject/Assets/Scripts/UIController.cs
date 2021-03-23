using UnityEngine;
using MLAPI;

public class UIController : MonoBehaviour
{
    public NetworkManager network;
    public GameObject buttonsUI;

    public void CreateServer()
    {
        network.StartServer();
        HideButtons();
    }

    public void CreateHost()
    {
        network.StartHost();
        HideButtons();
    }

    public void JoinGame()
    {
        network.StartClient();
        HideButtons();
    }

    private void HideButtons()
    {
        buttonsUI.SetActive(false);
    }
}