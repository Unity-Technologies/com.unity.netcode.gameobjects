using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Spawning;
using MLAPI.NetworkVariable;

public class InGameManager : NetworkBehaviour
{
    [SerializeField]
    private bool m_LaunchAsHostInEditor;

    [SerializeField]
    private bool m_ExitIfNoPlayers;

    [SerializeField]
    private Text m_WaitingForPlayers;

    [SerializeField]
    private Text m_GameIsPaused;

    [SerializeField]
    private Text m_ExitingGame;

    [SerializeField]
    private Text m_HostInfo;

    [SerializeField]
    [Range(1,10)]
    int m_ExitGameCountDown = 5; //Set default to 5

    public enum InGameStates
    {
        Waiting,
        Playing,
        Paused,
        Exiting
    }

    /// <summary>
    /// m_InGameState
    /// Networked Var Use Case Scenario:  State Machine
    /// Update Frequency: 0ms (immediate)
    /// Used for a state machine that updates immediately upon the value changing.
    /// Clients only have read access to the current GlobalGameState.
    /// </summary>
    private NetworkVariable<InGameStates> m_InGameState = new NetworkVariable<InGameStates>(new NetworkVariableSettings(){ WritePermission = NetworkVariablePermission.ServerOnly } , InGameStates.Waiting);

    /// <summary>
    /// m_ExitingTime
    /// Networked Var Use Case Scenario:  Timer
    /// Update Frequency: 0ms (immediate)
    /// This is used as a general network timer for things like exiting notifications or game startup count downs
    /// Clients only have read access to the current GlobalGameState.
    /// !!NOTE!! Leave the initial value > 0 to assure all clients have received the first full exiting timer update from the server
    /// </summary>
    private NetworkVariableFloat m_ExitingTime = new NetworkVariableFloat(new NetworkVariableSettings(){ WritePermission = NetworkVariablePermission.ServerOnly, ReadPermission = NetworkVariablePermission.Everyone } , 1.0f);

    /// <summary>
    /// OnInGameStateChangedDelegateHandler
    /// Used to create the OnInGameStateChanged event that other components can subscribe to
    /// Useful to change the state of objects that persist between scenes duringa an in-game session
    /// </summary>
    /// <param name="previousState"></param>
    /// <param name="newState"></param>
    public delegate void OnInGameStateChangedDelegateHandler(InGameStates previousState, InGameStates newState);
    public event OnInGameStateChangedDelegateHandler OnInGameStateChanged;

    //The local player
    private NetworkObject m_LocalPlayerObject;

    private void Awake()
    {
#if UNITY_EDITOR
        if ( NetworkManager.Singleton == null)
        {
            GlobalGameState.s_EditorLaunchingAsHost = m_LaunchAsHostInEditor;
            //This will automatically launch the MLAPIBootStrap and then transition directly to the scene this control is contained within (for easy development of scenes)
            GlobalGameState.LoadBootStrapScene();
            return;
        }
#endif
        if (NetworkManager.Singleton.IsListening)
        {
            if (IsServer)
            {
                GlobalGameState.Singleton.allPlayersLoadedScene += AllPlayersLoadedScene;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            }

            m_InGameState.OnValueChanged += InGameStateValueChanged;
        }
    }

    /// <summary>
    /// Start
    /// Handle the initialization of dialog (text) and registering callbacks
    /// </summary>
    void Start()
    {
        if (m_WaitingForPlayers)
        {
            m_WaitingForPlayers.enabled = true;
        }

        if (m_GameIsPaused)
        {
            m_GameIsPaused.enabled = false;
        }

        if (m_ExitingGame)
        {
            m_ExitingGame.enabled = false;
        }

        if (!IsServer && m_HostInfo)
        {
            m_HostInfo.enabled = false;
        }

        SetAndInitializeLocalPlayer();
        FreezeAndShowPlayers();
    }

    /// <summary>
    /// SetAndInitializeLocalPlayer
    /// SpawnManager.SpawnedObjects Use Case Scenario:  Parsing through currently spawned NetworkObjects
    /// Sets our local player, assigns the local main camera instanve to the local player, and
    /// registers all NetworkObjects with a RandomPlayerMover component for In-Game state changes
    /// </summary>
    void SetAndInitializeLocalPlayer()
    {
        foreach(NetworkObject networkedObject in NetworkSpawnManager.SpawnedObjects.Values)
        {
            RandomPlayerMover PlayerMover = networkedObject.GetComponent<RandomPlayerMover>();

            if (PlayerMover)
            {
                if (networkedObject.IsLocalPlayer)
                {
                    m_LocalPlayerObject = networkedObject;
                    PlayerMover.SetPlayerCamera();
                }
                PlayerMover.OnRegisterInGameManager(this);
            }
        }
    }

    /// <summary>
    /// FreezeAndShowPlayers
    /// SpawnManager.SpawnedObjects Use Case Scenario:  Parsing through currently spawned NetworkObjects
    /// This is to handle pausing and unpausing primarily the players' networked object clones not owned by the local player
    /// </summary>
    /// <param name="shouldFreeze">should we freeze everyone or un-freeze them?</param>
    void FreezeAndShowPlayers(bool shouldFreeze = false)
    {

        foreach(NetworkObject networkedObject in NetworkSpawnManager.SpawnedObjects.Values)
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
                PlayerMover.OnPaused(shouldFreeze);
                PlayerMover.OnIsHidden(false);
            }
        }
    }

    /// <summary>
    /// InGameStateValueChanged
    /// Invoked when the in-game state changes (not to be confused with the GlobalGameState
    /// </summary>
    /// <param name="previousState"></param>
    /// <param name="newState"></param>
    void InGameStateValueChanged(InGameStates previousState, InGameStates newState)
    {
        InGameStateTransition(previousState, false);
        InGameStateTransition(newState, true);

        if (OnInGameStateChanged != null)
        {
            OnInGameStateChanged.Invoke(previousState, newState);
        }
    }

    /// <summary>
    /// InGameStateTransition
    /// This is where you can change various aspects of your game based on whether
    /// you are transitioning to or from a specific in game state.
    /// The two examples provided are:
    /// --- waiting for players to join the in game session
    /// --- the game is paused (server only for this sample)
    /// </summary>
    /// <param name="gameState"></param>
    /// <param name="isTransitioningTo"></param>
    void InGameStateTransition(InGameStates gameState, bool isTransitioningTo)
    {
        switch(gameState)
        {
            case InGameStates.Waiting:
                {
                    if (m_WaitingForPlayers)
                    {
                        m_WaitingForPlayers.enabled = isTransitioningTo;
                        m_WaitingForPlayers.gameObject.SetActive(isTransitioningTo);
                    }
                    FreezeAndShowPlayers(isTransitioningTo);
                    break;
                }
            case InGameStates.Playing:
                {
                    break;
                }
            case InGameStates.Paused:
                {
                    if (m_GameIsPaused)
                    {
                        m_GameIsPaused.enabled = isTransitioningTo;
                        m_GameIsPaused.gameObject.SetActive(isTransitioningTo);
                    }

                    if (m_LocalPlayerObject)
                    {
                        //TODO: Create a different player object based on this sample's PlayerController
                        //For now, we just stop moving the cubes moving around
                        RandomPlayerMover movementRandomizer = m_LocalPlayerObject.GetComponent<RandomPlayerMover>();
                        if (movementRandomizer)
                        {
                            movementRandomizer.OnPaused(isTransitioningTo);
                        }
                    }
                    FreezeAndShowPlayers(isTransitioningTo);
                    break;
                }
            case InGameStates.Exiting:
                {
                    if (IsServer)
                    {
                        //As long as there are players, let's let them know the game is exiting/ending
                        if (NetworkManager.Singleton.ConnectedClientsList.Count > 1)
                        {
                             m_ExitingTime.Value = m_ExitGameCountDown;
                        }
                        else
                        {
                            //Otherwise, if there are no more players then just exit
                            m_ExitingTime.Value = 0;
                        }
                    }

                    FreezeAndShowPlayers(isTransitioningTo);

                    if (m_ExitingGame)
                    {
                        m_ExitingGame.enabled = isTransitioningTo;
                        m_ExitingGame.gameObject.SetActive(isTransitioningTo);
                    }
                    break;
                }
        }
    }

    /// <summary>
    /// AllPlayersLoadedScene
    /// Since we have passed the lobby state, we know that all players are connected and just waiting
    /// for them to all load their scenes.
    /// </summary>
    void AllPlayersLoadedScene()
    {
        if (IsServer)
        {
            m_InGameState.Value = InGameStates.Playing;
        }
    }

    /// <summary>
    /// OnClientDisconnectCallback
    /// Notififies the server that a client has disconnected
    /// </summary>
    /// <param name="clientId"></param>
    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClientsList.Count == 1 && m_ExitIfNoPlayers)
        {
            //Transition the in game state to exiting
            m_InGameState.Value = InGameStates.Exiting;
        }
    }

    /// <summary>
    /// Update
    /// We can use the Monobehaviour Update method to pump our in game networked state machine
    /// </summary>
    void Update()
    {
        switch(m_InGameState.Value)
        {
            case InGameStates.Waiting:
                {
                    WaitingUpdate();
                    break;
                }
            case InGameStates.Playing:
                {
                    PlayingUpdate();
                    break;
                }
            case InGameStates.Paused:
                {
                    PausedUpdate();
                    break;
                }
            case InGameStates.Exiting:
                {
                    OnExitingGame();
                    break;
                }
        }

        //Only the host calls the  ServerCommandInputUpdate method
        if (IsServer)
        {
            ServerCommandInputUpdate();
        }
    }

    /// <summary>
    /// PlayingUpdate
    /// Executed once per update when the m_InGameState.Value is InGameStates.Waiting
    /// You could animate something here while players wait
    /// </summary>
    void WaitingUpdate()
    {
        //Waiting update here
    }

    /// <summary>
    /// PlayingUpdate
    /// Executed once per update when the m_InGameState.Value is InGameStates.Playing
    /// All in game logic happens here
    /// </summary>
    void PlayingUpdate()
    {
        //Game update here
    }

    /// <summary>
    /// PausedUpdate
    /// Executed once per update when the m_InGameState.Value is InGameStates.Paused
    /// You could animate something here while the game is paused
    /// </summary>
    void PausedUpdate()
    {
        //Paused update here
    }

    /// <summary>
    /// OnExitingGame
    /// Executed once per update when the m_InGameState.Value is InGameStates.Exiting
    /// </summary>
    void OnExitingGame()
    {
        //Server is authoritative for the exiting timer
        if (IsServer)
        {
            m_ExitingTime.Value = Mathf.Max(0, m_ExitingTime.Value - Time.deltaTime);
        }

        //If no time is left, then exit
        if (m_ExitingTime.Value == 0)
        {
            //Server waits for no more players, and then exits the game
            if (IsServer && NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
            {
                //De-register from our events
                GlobalGameState.Singleton.allPlayersLoadedScene -= AllPlayersLoadedScene;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.ExitGame);
            }
            else if (!IsServer)
            {
                //Clients always exit immediately
                GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.ExitGame);
            }
        }
        else
        {
            m_ExitingGame.text = "Exiting in " + m_ExitingTime.Value.ToString() + " seconds!";
        }
    }

    /// <summary>
    /// OnExitGame
    /// Tied to the "X" button in the top right corner of the InGame Scene
    /// </summary>
    public void OnExitGame()
    {
        if (IsServer)
        {
            m_InGameState.Value = InGameStates.Exiting;
        }
        else
        {
            //Clients always exit immediately
            GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.ExitGame);
        }
    }

    public void OnNextLevel()
    {
        if(IsServer)
        {
            GlobalGameState.Singleton.SetGameState(GlobalGameState.GameStates.InGame);
        }
    }

    /// <summary>
    /// ServerCommandInputUpdate
    /// This is where you can add commands or detect key presses to perform server/host operations
    /// </summary>
    void ServerCommandInputUpdate()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            //Only allow the paused to be toggled when paused or when playing
            if (m_InGameState.Value == InGameStates.Paused)
            {
                m_InGameState.Value = InGameStates.Playing;
            }
            else if (m_InGameState.Value == InGameStates.Playing)
            {
                m_InGameState.Value = InGameStates.Paused;
            }
        }
    }
}
