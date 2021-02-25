using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.NetworkedVar;
using MLAPI.SceneManagement;

public class GlobalGameState : NetworkedBehaviour
{
    static public GlobalGameState Singleton { get; internal set; }

    [SerializeField]
    public string DefaultMainMenu = "StartMenu";

    [HideInInspector]
    public delegate void ClientLoadedSceneDelegateHandler(ulong clientId);
    [HideInInspector]
    public event ClientLoadedSceneDelegateHandler clientLoadedScene;

    [HideInInspector]
    public delegate void GameStateChangedDelegateHandler(GameStates previousState, GameStates newState);

    [HideInInspector]
    public event GameStateChangedDelegateHandler gameStateChanged;

    //Tracks our scene switching progress
    private SceneSwitchProgress m_SceneProgress;

    /// <summary>
    /// Example scene states
    /// </summary>
    public enum GameStates
    {
        None,               //Default
        Init,               //Depending upon size of main menu (or first scene, could be a video or the like)
        Menu,               //When the user has reached the main menu to the game
        Lobby,              //When the user has entered a game session lobby
        InGame,             //When the user has entered into the game session itself
        ExitGame,           //When the user is exiting the game session
        Shutdown            //When the system is shutting down
    }

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
            case GameStates.None:
                {
                    break;
                }
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
            switch(newState)
            {
                case GameStates.None:
                    {
                        SceneManager.LoadScene(DefaultMainMenu);
                        break;
                    }
                case GameStates.Init:
                    {
                        //Any "upon change" configurations go here
                        break;
                    }
                case GameStates.Menu:
                    {
                        //Any "upon change" configurations go here
                        break;
                    }
                case GameStates.Lobby:
                    {
                        m_GameState.Settings.WritePermission = NetworkedVarPermission.ServerOnly;
                        break;
                    }
                case GameStates.InGame:
                    {
                        //Reset our in-game timer
                        m_InGameTime.Value = 0.0f;
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
                        SetGameState(GameStates.None);
                        break;
                    }
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
    /// Start
    /// Kicks of the initialization state, this is where we load the first scene from the MLAPI_BootStrap
    /// </summary>
    private void Start()
    {
        //Server and clients always execute this
        SetGameState(GameStates.Init);
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
    /// <returns></returns>
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
