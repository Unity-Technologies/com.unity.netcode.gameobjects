using MLAPI;
using MLAPI.NetworkVariable;

public class GlobalGameStateComponent : NetworkBehaviour
{
    /// <summary>
    /// Networked Var Use Case Scenario:  State Machine
    /// Update Frequency: 0ms (immediate)
    /// Used for a state machine that updates immediately upon the value changing.
    /// Clients only have read access to the current GlobalGameState.
    /// </summary>
    private NetworkVariable<GlobalGameState.GameStates> m_GameState = new NetworkVariable<GlobalGameState.GameStates>(new NetworkVariableSettings(){ WritePermission = NetworkVariablePermission.ServerOnly } , GlobalGameState.GameStates.None);

    private void Awake()
    {
        m_GameState.OnValueChanged += OnGameStateChanged;
    }

    /// <summary>
    /// Sets the new game state
    /// </summary>
    /// <param name="newState"></param>
    public void SetNewGameState(GlobalGameState.GameStates newState)
    {
        if (IsServer)
        {
            if (m_GameState.Value != newState)
            {
                m_GameState.Value = newState;
            }
        }
    }

    /// <summary>
    /// Cients and Server can register for this in order to synchronize the global game state between all clients (including the host-client)
    /// </summary>
    /// <param name="previousState">from state</param>
    /// <param name="newState">to state</param>
    private void OnGameStateChanged(GlobalGameState.GameStates previousState, GlobalGameState.GameStates newState)
    {
        if (GlobalGameState.Singleton && NetworkManager.Singleton.IsListening)
        {
            GlobalGameState.Singleton.OnGameStateChanged(previousState, newState);
        }
    }
}
