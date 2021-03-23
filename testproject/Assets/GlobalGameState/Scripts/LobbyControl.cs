using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.UI;

public class LobbyControl : NetworkBehaviour
{
    [SerializeField]
    private bool m_LaunchAsHostInEditor;

    [SerializeField]
    private Text m_LobbyText;

    [Tooltip("This value only determines if the minimum numbers of players to start are present.  All players must hit the ready button to launch.")]
    [Range(1,16)]
    [SerializeField]
    private int m_MinPlayersToStart = 2;

    private Dictionary<ulong,bool> m_ClientsInLobby;
    private bool m_AllPlayersInLobby;
    private string m_UserLobbyStatusText;

    /// <summary>
    /// Initialize lobby related actions as well as handle in-editor invocation of bootstrap
    /// </summary>
    private void Awake()
    {
        m_ClientsInLobby = new Dictionary<ulong, bool>();
#if UNITY_EDITOR
        if (NetworkManager.Singleton == null)
        {
            GlobalGameState.s_EditorLaunchingAsHost = m_LaunchAsHostInEditor;
            //This will automatically launch the MLAPIBootStrap and then transition directly to the scene this control is contained within (for easy development of scenes)
            GlobalGameState.LoadBootStrapScene();
            return;
        }
#endif

        if (NetworkManager.Singleton.IsListening)
        {
            //Always add ourselves to the list at first
            m_ClientsInLobby.Add(NetworkManager.Singleton.LocalClientId, false);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            //If we are hosting, then handle the server side for detecting when clients have connected
            //and when their lobby scenes are finished loading.
            if (IsServer)
            {
                m_AllPlayersInLobby = false;
                //Server will be notified when a client connects

                GlobalGameState.Singleton.ClientLoadedScene += ClientLoadedScene;
            }
            //Update our lobby
            GenerateUserStatsForLobby();
        }
    }


    /// <summary>
    /// Just freeze and hide players upon starting
    /// </summary>
    private void Start()
    {
        FreezeAndHidePlayers();
    }


    /// <summary>
    /// This parses through all local NetworkObjects, freezes (pauses) them, and "hides" them.
    /// (one of several ways to do this)
    /// </summary>
    void FreezeAndHidePlayers()
    {
        NetworkObject[] NetoworkedObjects = GameObject.FindObjectsOfType<NetworkObject>();
        foreach (NetworkObject networkedObject in NetoworkedObjects)
        {
            RandomPlayerMover PlayerMover = networkedObject.GetComponent<RandomPlayerMover>();
            if (PlayerMover)
            {
                if (!IsOwner)
                {
                    AudioListener OtherAudioListener = networkedObject.GetComponent<AudioListener>();
                    if (OtherAudioListener)
                    {
                        OtherAudioListener.enabled = false;
                    }
                }
                PlayerMover.OnPaused(true);
                PlayerMover.OnIsHidden(true);
            }
        }
    }

    /// <summary>
    /// Psuedo code for setting player state
    /// Just updating a text field, this could use a lot of "refactoring"  :)
    /// </summary>
    private void GenerateUserStatsForLobby()
    {
        m_UserLobbyStatusText = string.Empty;
        foreach (KeyValuePair<ulong, bool> clientLobbyStatus in m_ClientsInLobby)
        {
            m_UserLobbyStatusText += "Player_" + clientLobbyStatus.Key.ToString() + "          ";
            if (clientLobbyStatus.Value)
            {
                m_UserLobbyStatusText += "(Ready)\n";
            }
            else
            {
                m_UserLobbyStatusText += "(Not Ready)\n";
            }
        }

        FreezeAndHidePlayers();
    }

    /// <summary>
    /// Update our lobby
    /// (this could be actual UI elements as opposed to just text)
    /// </summary>
    private void OnGUI()
    {
        if (m_LobbyText != null)
        {
            m_LobbyText.text = m_UserLobbyStatusText;
        }
    }

    /// <summary>
    /// Checks to see if we have at least m_MinPlayersToStart or more people to start
    /// </summary>
    void UpdateAndCheckPlayersInLobby()
    {
        //This is game preference, but I am assuming at least 2 players?
        m_AllPlayersInLobby = (m_ClientsInLobby.Count >= m_MinPlayersToStart);

        foreach (KeyValuePair<ulong, bool> clientLobbyStatus in m_ClientsInLobby)
        {
            SendClientReadyStatusUpdatesClientRpc(clientLobbyStatus.Key, clientLobbyStatus.Value);
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientLobbyStatus.Key))
            {
                //If some clients are still loading into the lobby scene then this is false
                m_AllPlayersInLobby = false;
            }
        }
        CheckForAllPlayersReady();
    }

    /// <summary>
    /// Invoked when a client has loaded this scene
    /// </summary>
    /// <param name="clientId"></param>
    private void ClientLoadedScene(ulong clientId)
    {
        if (IsServer)
        {
            if (!m_ClientsInLobby.ContainsKey(clientId))
            {
                m_ClientsInLobby.Add(clientId, false);
                GenerateUserStatsForLobby();
            }

            UpdateAndCheckPlayersInLobby();
        }
    }

    /// <summary>
    /// Since we are entering a lobby and MLAPI NetowrkingManager is spawning the player,
    /// the server can be configured to only listen for connected clients at this stage.
    /// </summary>
    /// <param name="clientId">client that connected</param>
    private void OnClientConnectedCallback(ulong clientId)
    {
        if (!m_ClientsInLobby.ContainsKey(clientId))
        {
            m_ClientsInLobby.Add(clientId, false);

        }
        if (IsServer)
        {
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            OnClientIsReadyServerRpc(NetworkManager.Singleton.LocalClientId, false);
        }
        GenerateUserStatsForLobby();
    }

    /// <summary>
    /// ClientRpc Use Case Scenario:  Sending clients updated information about other clients in the lobby
    /// Sent from the server to the client when a player's status is updated.
    /// This also populates the connected clients' (excluding host) player state in the lobby
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="isReady"></param>
    [ClientRpc]
    void SendClientReadyStatusUpdatesClientRpc(ulong clientId, bool isReady)
    {
        if (!IsServer)
        {
            if (!m_ClientsInLobby.ContainsKey(clientId))
            {
                m_ClientsInLobby.Add(clientId, isReady);
            }
            else
            {
                m_ClientsInLobby[clientId] = isReady;
            }
            GenerateUserStatsForLobby();
        }
    }

    /// <summary>
    /// Checks to see if all players are ready, and if so launches the game
    /// </summary>
    private void CheckForAllPlayersReady()
    {
        if (m_AllPlayersInLobby)
        {
            bool AllPlayersAreReady = true;
            foreach (KeyValuePair<ulong, bool> clientLobbyStatus in m_ClientsInLobby)
            {
                if (!clientLobbyStatus.Value)
                {
                    //If some clients are still loading into the lobby scene then this is false
                    AllPlayersAreReady = false;
                }
            }

            //Only if all players are ready
            if (AllPlayersAreReady)
            {
                //Remove our client connected callback
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

                //Remove our scene loaded callback
                GlobalGameState.Singleton.ClientLoadedScene -= ClientLoadedScene;

                //Transition to the ingame scene
                GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.InGame);
            }
        }
    }

    /// <summary>
    /// Tied to the Ready button in the lobby scene
    /// </summary>
    public void PlayerIsReady()
    {
        if (IsServer)
        {
            m_ClientsInLobby[NetworkManager.Singleton.ServerClientId] = !m_ClientsInLobby[NetworkManager.Singleton.ServerClientId];
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            m_ClientsInLobby[NetworkManager.Singleton.LocalClientId] = !m_ClientsInLobby[NetworkManager.Singleton.LocalClientId];
            OnClientIsReadyServerRpc(NetworkManager.Singleton.LocalClientId, m_ClientsInLobby[NetworkManager.Singleton.LocalClientId]);
        }
        GenerateUserStatsForLobby();
    }

    /// <summary>
    /// ServerRpc Use Case Scenario: Clients notifying the server of their state, and do not require ownership of the object
    /// Sent to the server when the player clicks the ready button
    /// </summary>
    /// <param name="clientid">clientId that is ready</param>
    [ServerRpc(RequireOwnership = false)]
    void OnClientIsReadyServerRpc(ulong clientid, bool isReady)
    {
        if (m_ClientsInLobby.ContainsKey(clientid))
        {
            m_ClientsInLobby[clientid] = isReady;
            UpdateAndCheckPlayersInLobby();
            GenerateUserStatsForLobby();
        }
    }

    /// <summary>
    /// OnExitLobby
    /// Transitions the local state (unless the server) to exiting, which will exit the game session and will load the scene linked to the GlobalGameState.GameStates.ExitGame state
    /// </summary>
    public void OnExitLobby()
    {
        GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.ExitGame);
    }

    /// <summary>
    /// OnDestroy
    /// Make sure to remove ourself from the client connected and client loaded scene callbacks
    /// </summary>
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            if (IsServer)
            {
                GlobalGameState.Singleton.ClientLoadedScene -= ClientLoadedScene;
            }
        }
    }
}
