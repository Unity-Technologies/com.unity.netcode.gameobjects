using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.NetworkedVar;
using MLAPI.SceneManagement;



[ExecuteInEditMode]
public class GlobalGameState : NetworkedBehaviour
{
   static public GlobalGameState Singleton { get; internal set; }

    [Header("Window Resolution")]

    [SerializeField]
    [Tooltip("Is the game going to always run in full screen?")]
    private bool m_IsFullScreen;

    [SerializeField]
    [Tooltip("Horizontal window resolution size (if fullscreen this is ignored)")]
    private int m_HorizontalResolution = 1024;

    [SerializeField]
    [Tooltip("Vertical window resolution size (if fullscreen this is ignored)")]
    private int m_VerticalResolution = 768;

    [Space]
    /// <summary>
    /// The name of the scene that is the default main menu
    /// </summary>
    [SerializeField]
    private StateToSceneTransitionList SceneToStateLinks;

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
    /// Networked Var Use Case Scenario:  State Machine
    /// Update Frequency: 0ms (immediate)
    /// Used for a state machine that updates immediately upon the value changing.
    /// Clients only have read access to the current GlobalGameState.
    /// </summary>
    private NetworkedVar<GameStates> m_GameState = new NetworkedVar<GameStates>(new NetworkedVarSettings(){ WritePermission = NetworkedVarPermission.ServerOnly } , GameStates.Init);

    /// <summary>
    /// Networked Var Use Case Scenario: (n) frequency resolution timer
    /// Update Frequency: 10fps (100ms frequency resolution)
    /// Can be used for various global timing events
    /// Clients only have read access
    /// </summary>
    private NetworkedVarFloat m_InGameTime = new NetworkedVarFloat(new NetworkedVarSettings(){ SendTickrate = 0.100f, WritePermission = NetworkedVarPermission.ServerOnly } , 0.0f);

    /// <summary>
    /// Networked Var Use Case Scenario:  public read only accessor to NetworkedVarFloat m_InGameTime
    /// One way to expose a global value that can be updated regularly
    /// Clients only have read access
    /// </summary>
    public float inGameTime { get{ return m_InGameTime.Value; } }
    #endregion

    //Tracks our scene switching progress
    private SceneSwitchProgress m_SceneProgress;

    /// <summary>
    /// GameStates
    /// Relates to:  NetworkedVar<GameStates> m_GameState
    /// Provides the different states for the networked game state machine
    /// </summary>
    public enum GameStates
    {
        Init,               //The initial state
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
        Screen.SetResolution(m_HorizontalResolution, m_VerticalResolution, false);

        StateToSceneTransitionLinks.Initialize();

        //Only invoke this when the application is playing (i.e. do not execute this code within the editor in edit mode)
        if(Application.isPlaying)
        {
            //Server and clients always execute this
            SetGameState(GameStates.Init);
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
        if(gameStateChanged != null)
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
                    if(m_GameState.Settings.WritePermission != NetworkedVarPermission.ServerOnly)
                    {
                         m_GameState.Settings.WritePermission = NetworkedVarPermission.ServerOnly;
                    }
                    break;
                }
            case GameStates.InGame:
                {
                    if(IsServer)
                    {
                        m_InGameTime.Value += Time.deltaTime;
                    }
                    break;
                }
            case GameStates.ExitGame:
                {
                    //Clean up
                    if(m_SceneProgress != null)
                    {
                        m_SceneProgress = null;
                    }
                    if(clientLoadedScene != null)
                    {
                        clientLoadedScene = null;
                    }

                    //Revert back to everyone access after we leave the game session
                    m_GameState.Settings.WritePermission = NetworkedVarPermission.Everyone;

                    //one level deep recursive call to start loading the main menu again
                    SetGameState(GameStates.Menu);
                    break;
                }
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
        if(previousState != newState)
        {
            string SceneName = SceneToStateLinks.GetSceneNameLinkedToState(newState);
            if(SceneName != string.Empty)
            {
                SwitchScene(SceneName);
            }

            //make sure we can set this networked variable
            if(IsServer || m_GameState.Settings.WritePermission != NetworkedVarPermission.ServerOnly)
            {
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
    public void SwitchScene(string scenename)
    {
        // If we have started our network transport (connecting, connected, in game session, etc)
        if(NetworkingManager.Singleton.IsListening)
        {
            //Use the NetworkSceneManager
            m_SceneProgress = NetworkSceneManager.SwitchScene(scenename);

            //Use the SceneSwitchProgress OnClientLoadedScene event
            m_SceneProgress.OnClientLoadedScene += SceneProgress_OnClientLoadedScene;
        }
        else
        {
            //If transport is not active then just load the scene via the normal Unity scene manager
            SceneManager.LoadSceneAsync(scenename);
        }
    }

    /// <summary>
    /// AllClientsAreLoaded
    /// Returns whether all lients are loaded
    /// </summary>
    /// <returns>true or false (they are all loaded or they are not)</returns>
    public bool AllClientsAreLoaded()
    {
        if(m_SceneProgress != null)
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
        if(clientLoadedScene != null)
        {
            clientLoadedScene.Invoke(clientId);
        }
    }
}
