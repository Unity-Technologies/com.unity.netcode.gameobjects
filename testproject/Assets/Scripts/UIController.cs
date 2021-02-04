using System.Collections;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;

public class UIController : MonoBehaviour
{
  public NetworkingManager network;

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

  void HideButtons()
  {
    buttonsUI.SetActive(false);
  }
}
