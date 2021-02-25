using System.IO;
using System.Collections.Generic;
using MLAPI;
using MLAPI.SceneManagement;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.UI;

public class LobbyControl : NetworkedBehaviour
{
    [HideInInspector]
    public static bool isHosting;

    [SerializeField]
    private string m_InGameSceneName = "InGame";
    public Text LobbyText;

    private Dictionary<ulong,bool> m_ClientsInLobby;
    private bool m_AllPlayersInLobby;
    private string m_UserLobbyStatusText;

    /// <summary>
    /// Awake
    /// This is one way to kick off a multiplayer session
    /// </summary>
    private void Awake()
    {
        m_ClientsInLobby = new Dictionary<ulong, bool>();

        //We added this information to tell us if we are going to host a game or join an the game session
        if (isHosting)
        {
            NetworkingManager.Singleton.StartHost();  //Spin up the host
        }
        else
        {
            NetworkingManager.Singleton.StartClient();//Spin up the client
        }

        if(NetworkingManager.Singleton.IsListening)
        {
            //Always add ourselves to the list at first
            m_ClientsInLobby.Add(NetworkingManager.Singleton.LocalClientId, false);

            //If we are hosting, then handle the server side for detecting when clients have connected
            //and when their lobby scenes are finished loading.
            if(IsServer)
            {
                m_AllPlayersInLobby = false;
                //Server will be notified when a client connects
                NetworkingManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                GlobalGameState.Singleton.clientLoadedScene += ClientLoadedScene;
            }
            //Update our lobby
            GenerateUserStatsForLobby();
        }
    }


    /// <summary>
    /// GenerateUserStatsForLobby
    /// Psuedo code for setting player state
    /// Just updating a text field, this could use a lot of "refactoring"  :)
    /// </summary>
    private void GenerateUserStatsForLobby()
    {
        m_UserLobbyStatusText = string.Empty;
        foreach(KeyValuePair<ulong,bool> clientLobbyStatus in m_ClientsInLobby)
        {
            m_UserLobbyStatusText += "Player_" + clientLobbyStatus.Key.ToString() + "          ";
            if(clientLobbyStatus.Value)
            {
                m_UserLobbyStatusText += "(Ready)\n";
            }
            else
            {
                m_UserLobbyStatusText += "(Not Ready)\n";
            }
        }
    }

    private void OnGUI()
    {
        if(LobbyText != null)
        {
            LobbyText.text = m_UserLobbyStatusText;
        }
    }

    /// <summary>
    /// UpdateAndCheckPlayersInLobby
    /// Checks to see if we have at least 2 or more people to start
    /// </summary>
    void UpdateAndCheckPlayersInLobby()
    {
        //This is game preference, but I am assuming at least 2 players?
        m_AllPlayersInLobby = m_ClientsInLobby.Count > 1;

        foreach(KeyValuePair<ulong,bool> clientLobbyStatus in m_ClientsInLobby)
        {
            SendClientReadyStatusUpdatesClientRpc(clientLobbyStatus.Key, clientLobbyStatus.Value);
            if(!NetworkingManager.Singleton.ConnectedClients.ContainsKey(clientLobbyStatus.Key))
            {
                //If some clients are still loading into the lobby scene then this is false
                m_AllPlayersInLobby = false;
            }
        }
        CheckForAllPlayersReady();
    }

    /// <summary>
    /// ClientLoadedScene
    /// Invoked when a client has loaded this scene
    /// </summary>
    /// <param name="clientId"></param>
    private void ClientLoadedScene(ulong clientId)
    {
        if(IsServer)
        {
            if(!m_ClientsInLobby.ContainsKey(clientId))
            {
                m_ClientsInLobby.Add(clientId, false);
                GenerateUserStatsForLobby();
            }

            UpdateAndCheckPlayersInLobby();
        }
    }

    /// <summary>
    /// OnClientConnectedCallback
    /// Since we are entering a lobby and MLAPI NetowrkingManager is spawning the player,
    /// the server can be configured to only listen for connected clients at this stage.
    /// </summary>
    /// <param name="clientId">client that connected</param>
    private void OnClientConnectedCallback(ulong clientId)
    {
        if(IsServer)
        {
            if(!m_ClientsInLobby.ContainsKey(clientId))
            {
                m_ClientsInLobby.Add(clientId, false);
            }
            GenerateUserStatsForLobby();

            UpdateAndCheckPlayersInLobby();
        }
    }



    /// <summary>
    /// SendClientReadyStatusUpdatesClientRpc
    /// Sent from the server to the client when a player's status is updated.
    /// This also populates the connected clients' (excluding host) player state in the lobby
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="isReady"></param>
    [ClientRpc]
    void SendClientReadyStatusUpdatesClientRpc(ulong clientId, bool isReady)
    {
        if(!IsServer)
        {
            if(!m_ClientsInLobby.ContainsKey(clientId))
            {
                m_ClientsInLobby.Add(clientId,isReady);
            }
            else
            {
                 m_ClientsInLobby[clientId] = isReady;
            }
            GenerateUserStatsForLobby();
        }
    }

    /// <summary>
    /// CheckForAllPlayersReady
    /// Checks to see if all players are ready, and if so launches the game
    /// </summary>
    private void CheckForAllPlayersReady()
    {
        if(m_AllPlayersInLobby)
        {
            bool AllPlayersAreReady = true;
            foreach(KeyValuePair<ulong,bool> clientLobbyStatus in m_ClientsInLobby)
            {
                if(!clientLobbyStatus.Value)
                {
                    //If some clients are still loading into the lobby scene then this is false
                    AllPlayersAreReady = false;
                }
            }

            //Only if all players are ready
            if(AllPlayersAreReady)
            {
                //Remove our client connected callback
                NetworkingManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

                //Remove our scene loaded callback
                GlobalGameState.Singleton.clientLoadedScene -= ClientLoadedScene;

                //Transition to the ingame scene
                GlobalGameState.Singleton.SwitchScene(m_InGameSceneName);
            }
        }
    }

    /// <summary>
    /// PlayerIsReady
    /// Tied to the Ready button in the InvadersLobby scene
    /// </summary>
    public void PlayerIsReady()
    {
        if(IsServer)
        {
            m_ClientsInLobby[NetworkingManager.Singleton.ServerClientId] = true;
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            m_ClientsInLobby[NetworkingManager.Singleton.LocalClientId] = true;
            OnClientIsReadyServerRpc(NetworkingManager.Singleton.LocalClientId);
        }

        GenerateUserStatsForLobby();
    }

    /// <summary>
    /// OnClientIsReadyServerRpc
    /// Sent to the server when the player clicks the ready button
    /// </summary>
    /// <param name="clientid">clientId that is ready</param>
    [ServerRpc(RequireOwnership = false)]
    void OnClientIsReadyServerRpc(ulong clientid)
    {
        if(m_ClientsInLobby.ContainsKey(clientid))
        {
            m_ClientsInLobby[clientid] = true;
            UpdateAndCheckPlayersInLobby();
            GenerateUserStatsForLobby();
        }
    }
}
