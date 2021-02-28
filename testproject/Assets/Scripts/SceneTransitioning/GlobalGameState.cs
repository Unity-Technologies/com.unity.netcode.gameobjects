using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.NetworkedVar;
using MLAPI.SceneManagement;
using MLAPIGlobalGameState;


public class GlobalGameState : NetworkedBehaviour
{
    static public GlobalGameState Singleton { get; internal set; }

    [Header("Window Resolution")]

    [SerializeField]
    [Tooltip("Is the game going start in full screen mode?")]
    private bool m_IsFullScreen;

    [SerializeField]
    [Tooltip("If set, the user cannot hit <alt><enter> and get into windowed mode.")]
    private bool m_IsExclusive;

    [SerializeField]
    [Tooltip("If set, the maximum resolution will be used (HorizontalResolution and VerticalResolution will be ignored).")]
    private bool m_MaximumResolution;

    [SerializeField]
    [Tooltip("Horizontal window resolution size (if MaximumResolution this is ignored)")]
    private int m_HorizontalResolution = 1024;

    [SerializeField]
    [Tooltip("Vertical window resolution size (if MaximumResolution this is ignored)")]
    private int m_VerticalResolution = 768;

    [Space]
    /// <summary>
    /// The name of the scene that is the default main menu
    /// </summary>
    [SerializeField]
    private StateToSceneTransitionList m_SceneToStateLinks;

    #region Events and Delegate Handlers
    [HideInInspector]
    public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);

    /// <summary>
    /// clientLoadedScene (Event)
    /// Usage:  typically the host-server subscribes to this event to be notified
    /// when each individual player has loaded a scene
    /// </summary>
    [HideInInspector]
    public event ClientLoadedSceneDelegateHandler clientLoadedScene;

    [HideInInspector]
    public delegate void GameStateChangedDelegateHandler(GameStates previousState, GameStates newState);

    /// <summary>
    /// gameStateChanged (Event)
    /// Usage: everyone (clients and serve) can subscribe to this event
    /// to be notified when the global game state changes
    /// </summary>
    [HideInInspector]
    public event GameStateChangedDelegateHandler gameStateChanged;

    [HideInInspector]
    public delegate void AllPlayersLoadedSceneDelegateHandler();

    /// <summary>
    /// allPlayersLoadedScene (Event)
    /// Usage: typically the host-server subscribes to this event to be notified
    /// that all players have finished loading a scene
    /// </summary>
    [HideInInspector]
    public event AllPlayersLoadedSceneDelegateHandler allPlayersLoadedScene;
    #endregion

    #region NETWORK VARIABLES
    /// <summary>
    /// m_GameState
    /// Networked Var Use Case Scenario:  State Machine
    /// Update Frequency: 0ms (immediate)
    /// Used for a state machine that updates immediately upon the value changing.
    /// Clients only have read access to the current GlobalGameState.
    /// </summary>
    private NetworkedVar<GameStates> m_GameState = new NetworkedVar<GameStates>(new NetworkedVarSettings(){ WritePermission = NetworkedVarPermission.ServerOnly } , GameStates.None);

    /// <summary>
    /// m_InGameTime
    /// Networked Var Use Case Scenario: (n) frequency resolution timer
    /// Update Frequency: 10fps (100ms frequency resolution)
    /// Can be used for various global timing events
    /// Clients only have read access
    /// </summary>
    private NetworkedVarFloat m_InGameTime = new NetworkedVarFloat(new NetworkedVarSettings(){ SendTickrate = 0.100f, WritePermission = NetworkedVarPermission.ServerOnly } , 0.0f);

    /// <summary>
    /// inGameTime
    /// Networked Var Use Case Scenario:  public read only accessor to NetworkedVarFloat m_InGameTime
    /// One way to expose a global value that can be updated regularly
    /// Clients only have read access
    /// </summary>
    public float inGameTime { get{ return m_InGameTime.Value; } }
    #endregion

    //This is set prior to setting any game state that activates MLAPI
    [HideInInspector]
    public bool isHostingGame;

    //Used to determine the current state of MLAPI (Global Game State Relative)
    private StateToSceneTransitionLinks.MLAPIStates m_CurrentMLAPIState;

    //Tracks our scene switching progress
    private SceneSwitchProgress m_SceneProgress;

    /// <summary>
    /// GameStates
    /// Relates to:  NetworkedVar<GameStates> m_GameState
    /// Provides the different states for the networked game state machine
    /// </summary>
    public enum GameStates
    {
        None,               //The initial state
        Init,               //Optional game intro screen, video, or additional splash sceens
        Menu,               //When the user has reached the main menu to the game
        Lobby,              //When the user has entered a game session lobby
        InGame,             //When the user has entered into the game session itself
        ExitGame,           //When the user is exiting the game session
        Shutdown            //When the system is shutting down
    }

    /// <summary>
    /// Awake
    /// If another version exists, destroy it and use the current version
    /// Set our scene state to INIT
    /// </summary>
    private void Awake()
    {
        if(Singleton != this && Singleton != null)
        {
            GameObject.Destroy(Singleton.gameObject);
        }
        Singleton = this;

        m_GameState.OnValueChanged += OnGameStateChanged;
    }

    /// <summary>
    /// Start
    /// Kicks of the initialization state, this is where we load the first scene from the MLAPI_BootStrap
    /// </summary>
    private void Start()
    {
        if (!m_IsExclusive)
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }

        if (m_MaximumResolution)
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, m_IsFullScreen);
        }
        else
        {
            Screen.SetResolution(m_HorizontalResolution, m_VerticalResolution, m_IsFullScreen);
        }

        //Only invoke this when the application is playing (i.e. do not execute this code within the editor in edit mode)
        if (Application.isPlaying)
        {
            if (!CheckForBootStrappedScene())
            {
                //Server and clients always execute this
                SetGameState(GameStates.Init);
            }
        }
    }

    /// <summary>
    /// OnGameStateChanged
    /// Cients and Server can register for this in order to synchronize the global game state between all clients (including the host-client)
    /// </summary>
    /// <param name="previousState">from state</param>
    /// <param name="newState">to state</param>
    private void OnGameStateChanged(GameStates previousState, GameStates newState)
    {
        if (gameStateChanged != null)
        {
            gameStateChanged.Invoke(previousState, newState);
        }
    }

    /// <summary>
    /// MonoBehaviour.Update
    /// Used primarily to update anything specific to a Global Game State
    /// </summary>
    private void Update()
    {
        //Any per frame tasks can be performed here (this is not required but can be useful for things like time outs and such)
        switch(m_GameState.Value)
        {
            case GameStates.Init:
                {
                    break;
                }
            case GameStates.Menu:
                {
                    break;
                }
            case GameStates.Lobby:
                {
                    if (m_GameState.Settings.WritePermission != NetworkedVarPermission.ServerOnly)
                    {
                         m_GameState.Settings.WritePermission = NetworkedVarPermission.ServerOnly;
                    }
                    break;
                }
            case GameStates.InGame:
                {
                    if (IsServer)
                    {
                        m_InGameTime.Value += Time.deltaTime;
                    }
                    break;
                }
            case GameStates.ExitGame:
                {
                    //one level deep recursive call to start loading the main menu again
                    SetGameState(GameStates.Menu);
                    break;
                }
        }
    }

    /// <summary>
    /// UpdateMLAPIState
    /// Handles the spinning up and shutting down of MLAPI
    /// </summary>
    /// <param name="newState">new state we are transitioning to</param>
    private void UpdateMLAPIState(GameStates newState)
    {
        StateToSceneTransitionLinks.MLAPIStates NewMLAPIState = m_SceneToStateLinks.GetGameStateToMLAPIState(newState);
        if(m_CurrentMLAPIState != NewMLAPIState )
        {
            switch(NewMLAPIState)
            {
                case StateToSceneTransitionLinks.MLAPIStates.InSession:
                case StateToSceneTransitionLinks.MLAPIStates.Connecting:
                    {
                        if(m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.None)
                        {
                            if(!NetworkingManager.Singleton.IsListening)
                            {
                                //If we are host, then start the host
                                if (isHostingGame)
                                {
                                    NetworkingManager.Singleton.StartHost();  //Spin up the host
                                }
                                else //otherwise start the client
                                {
                                    NetworkingManager.Singleton.StartClient();//Spin up the client
                                }
                            }
                        }
                        break;
                    }
                case StateToSceneTransitionLinks.MLAPIStates.ExitSession:
                    {
                        if(m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.InSession || m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.Connecting)
                        {
                            if(NetworkingManager.Singleton.IsListening)
                            {
                                //If we are host, then stop the host
                                if (isHostingGame)
                                {
                                    isHostingGame = false;
                                    NetworkingManager.Singleton.StopHost();  //shutdown the host
                                }
                                else //otherwise stop the client
                                {
                                    NetworkingManager.Singleton.StopClient();//shutdown the client
                                }

                                //Clean up scene loading and progeess handlers
                                if(m_SceneProgress != null)
                                {
                                    m_SceneProgress = null;
                                }
                                if(clientLoadedScene != null)
                                {
                                    clientLoadedScene = null;
                                }
                            }
                        }
                        break;
                    }
            }
            m_CurrentMLAPIState = NewMLAPIState;
        }
    }

    /// <summary>
    /// GameStateChangedUpdate
    /// Any additional processing we might want to do globally depending upon the current state
    /// </summary>
    /// <param name="previousState">from state</param>
    /// <param name="newState">to state</param>
    private void GameStateChangedUpdate(GameStates previousState, GameStates newState)
    {
        //We only should update once per changed state
        if (previousState != newState)
        {

            string SceneName = m_SceneToStateLinks.GetSceneNameLinkedToState(newState);

            if (SceneName != string.Empty)
            {
                if (m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.InSession || m_CurrentMLAPIState == StateToSceneTransitionLinks.MLAPIStates.Connecting)
                {
                    //If we are in a session or connecting, then update the state first then switch scenes
                    UpdateMLAPIState(newState);
                    //Start the scene switch first
                    SwitchScene(SceneName, newState);

                }
                else
                {
                    //Then update the MLAPI state
                    UpdateMLAPIState(newState);

                    //Start the scene switch first
                    SwitchScene(SceneName, newState);
                }
            }

            //make sure we can set this networked variable
            if (IsServer || m_GameState.Settings.WritePermission != NetworkedVarPermission.ServerOnly || newState == GameStates.ExitGame )
            {
                if (newState == GameStates.ExitGame)
                {
                    //Revert back to everyone access after we leave the game session
                    m_GameState.Settings.WritePermission = NetworkedVarPermission.Everyone;
                }
                //Now set the state so all clients will receive the message
                m_GameState.Value = newState;
            }

        }
    }

    /// <summary>
    /// SetGameState (Server Only)
    /// Sets the current game state
    /// </summary>
    /// <param name="sceneState"></param>
    public void SetGameState(GameStates gameState)
    {
        GameStateChangedUpdate(m_GameState.Value, gameState);
    }

    /// <summary>
    /// GetCurrentSceneState
    /// Returns the current scene state
    /// </summary>
    /// <returns>current scene state</returns>
    public GameStates GetCurrentSceneState()
    {
        return m_GameState.Value;
    }

    /// <summary>
    /// Switches to a new scene
    /// </summary>
    /// <param name="scenename"></param>
    private void SwitchScene(string scenename,GameStates transitionState )
    {
        // If we have started our network transport (connecting, connected, in game session, etc)
        if (NetworkingManager.Singleton.IsListening && IsServer)
        {
            //Use the NetworkSceneManager
            m_SceneProgress = NetworkSceneManager.SwitchScene(scenename);

            //Use the SceneSwitchProgress OnClientLoadedScene event
            m_SceneProgress.OnClientLoadedScene += SceneProgress_OnClientLoadedScene;

            m_SceneProgress.OnComplete += OnLoadingComplete;
        }
        else
        {
            if (!NetworkingManager.Singleton.IsListening)
            {
                //If transport is not active then just load the scene via the normal Unity scene manager
                SceneManager.LoadScene(scenename);
            }
        }
    }

    /// <summary>
    /// OnLoadingComplete (Server Only)
    /// When all clients have loaded their scene, this event is invoked
    /// </summary>
    /// <param name="timedOut">whether it timed out or not</param>
    private void OnLoadingComplete(bool timedOut)
    {
        //TODO: Handle message about timeOut
        if (m_SceneProgress != null && m_SceneProgress.IsAllClientsDoneLoading)
        {
            if(allPlayersLoadedScene != null)
            {
                allPlayersLoadedScene.Invoke();
            }
        }
    }

    /// <summary>
    /// AllClientsAreLoaded
    /// Returns whether all lients are loaded
    /// </summary>
    /// <returns>true or false (they are all loaded or they are not)</returns>
    public bool AllClientsAreLoaded()
    {
        if (m_SceneProgress != null)
        {
            return m_SceneProgress.IsAllClientsDoneLoading;
        }
        return false;
    }

    /// <summary>
    /// Invoked when a client has finished loading a scene
    /// </summary>
    /// <param name="clientId"></param>
    private void SceneProgress_OnClientLoadedScene(ulong clientId)
    {
        if (clientLoadedScene != null)
        {
            clientLoadedScene.Invoke(clientId);
        }

    }

    #region IN-EDITOR SPECIFIC METHODS
    public static bool IsLoadingFromEditor()
    {
#if (UNITY_EDITOR)
        return true;
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    private static string BootStrapToScene;
    public static bool EditorLaunchingAsHost;
#endif

    public static bool CheckForBootStrappedScene()
    {
#if UNITY_EDITOR
        if(BootStrapToScene != null && BootStrapToScene != string.Empty)
        {
            GameStates BootStrappedGameState = Singleton.m_SceneToStateLinks.GetGameStateLinkedToScene(BootStrapToScene);
            BootStrapToScene = string.Empty;
            if(BootStrappedGameState != GameStates.None)
            {
                Singleton.isHostingGame = EditorLaunchingAsHost;
                Singleton.UpdateMLAPIState(BootStrappedGameState);
                Singleton.SetGameState(BootStrappedGameState);
                return true;
            }
        }
#endif
        return false;
    }

    public static void LoadBootStrapScene()
    {
#if UNITY_EDITOR
        foreach (UnityEditor.EditorBuildSettingsScene nextscene in UnityEditor.EditorBuildSettings.scenes)
        {
            if (nextscene.enabled)
            {
                string SceneFilename = nextscene.path.Substring(nextscene.path.LastIndexOf('/')+1);
                string[] SplitSceneFileName = SceneFilename.Split('.');
                if(SplitSceneFileName[0] == "MLAPIBootStrap")
                {
                    Scene currentScene = SceneManager.GetActiveScene();
                    if(currentScene != null)
                    {
                        BootStrapToScene = currentScene.name;
                    }
                    SceneManager.LoadScene(SplitSceneFileName[0]);
                }
            }
        }
#endif
    }
    #endregion
}
