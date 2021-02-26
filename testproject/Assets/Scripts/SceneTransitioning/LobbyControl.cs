using System.IO;
using System.Collections.Generic;
using MLAPI;
using MLAPI.SceneManagement;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.UI;

public class LobbyControl : NetworkedBehaviour
{
    public Text LobbyText;

    [Tooltip("This value only determines if the minimum numbers of players to start are present.  All players must hit the ready button to launch.")]
    [Range(1,16)]
    private int m_MinPlayersToStart = 2;

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
#if(UNITY_EDITOR)
        if( NetworkingManager.Singleton == null)
        {
            //This will automatically launch the MLAPIBootStrap and then transition directly to the scene this control is contained within (for easy development of scenes)
            GlobalGameState.LoadBootStrapScene();
            return;
        }
#endif

        if (NetworkingManager.Singleton.IsListening)
        {
            //Always add ourselves to the list at first
            m_ClientsInLobby.Add(NetworkingManager.Singleton.LocalClientId, false);

             NetworkingManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            //If we are hosting, then handle the server side for detecting when clients have connected
            //and when their lobby scenes are finished loading.
            if(IsServer)
            {
                m_AllPlayersInLobby = false;
                //Server will be notified when a client connects

                GlobalGameState.Singleton.clientLoadedScene += ClientLoadedScene;
            }
            //Update our lobby
            GenerateUserStatsForLobby();
        }
    }

    private void Start()
    {

    }

    public override void NetworkStart()
    {



        base.NetworkStart();
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
        m_AllPlayersInLobby = (m_ClientsInLobby.Count >= m_MinPlayersToStart);

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
        if(!m_ClientsInLobby.ContainsKey(clientId))
        {
            m_ClientsInLobby.Add(clientId, false);
             GenerateUserStatsForLobby();
        }
        if(IsServer)
        {
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            OnClientIsReadyServerRpc(NetworkingManager.Singleton.LocalClientId, false);
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
                GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.InGame);
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
            m_ClientsInLobby[NetworkingManager.Singleton.ServerClientId] = !m_ClientsInLobby[NetworkingManager.Singleton.ServerClientId];
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            m_ClientsInLobby[NetworkingManager.Singleton.LocalClientId] =  !m_ClientsInLobby[NetworkingManager.Singleton.LocalClientId];
            OnClientIsReadyServerRpc(NetworkingManager.Singleton.LocalClientId,m_ClientsInLobby[NetworkingManager.Singleton.LocalClientId]);
        }

        GenerateUserStatsForLobby();
    }

    /// <summary>
    /// OnClientIsReadyServerRpc
    /// Sent to the server when the player clicks the ready button
    /// </summary>
    /// <param name="clientid">clientId that is ready</param>
    [ServerRpc(RequireOwnership = false)]
    void OnClientIsReadyServerRpc(ulong clientid, bool isReady)
    {
        if(m_ClientsInLobby.ContainsKey(clientid))
        {
            m_ClientsInLobby[clientid] = isReady;
            UpdateAndCheckPlayersInLobby();
            GenerateUserStatsForLobby();
        }
    }


    private void OnDestroy()
    {
        NetworkingManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        if(IsServer)
        {
            GlobalGameState.Singleton.clientLoadedScene -= ClientLoadedScene;
        }

        Debug.Log("Destroying Lobby Control Object!");
    }
}
