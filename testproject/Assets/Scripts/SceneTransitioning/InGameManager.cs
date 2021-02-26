using System.IO;
using System.Collections.Generic;
using MLAPI;
using MLAPI.SceneManagement;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.UI;

public class InGameManager : NetworkedBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(NetworkingManager.Singleton.IsListening)
        {
            NetworkingManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (!IsServer && NetworkingManager.Singleton.LocalClientId == clientId)
        {
            OnExitGame();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnExitGame()
    {
        GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.ExitGame);
    }
}
