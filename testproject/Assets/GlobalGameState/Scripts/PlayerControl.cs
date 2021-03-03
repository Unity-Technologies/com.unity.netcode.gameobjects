using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

public class PlayerControl : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float m_MoveSpeed = 3.5f;

    [Header("Allowed Axial Motion")]
    [SerializeField]
    private bool m_X_Axis = true;
    [SerializeField]
    private bool m_Y_Axis = true;
    [SerializeField]
    private bool m_Z_Axis = true;


    [Header("Player Settings")]
    [SerializeField]
    //Example use case scenario for a slowly ( once per second ) updated networked variable
    NetworkVariableString m_PlayerName = new NetworkVariableString(new NetworkVariableSettings(){ SendTickrate = 1.0f } ,"Player_Name");

    /// <summary>
    /// m_Health
    /// Networked Var Use Case Scenario:  Tracking player health
    /// Update Frequency: 100ms
    /// Used to update the player's health
    /// Everyone but the server has read only access
    /// </summary>
    [SerializeField]
    //Example use case scenario for an immediate update server-side only (athoritative server) write capabilities
    NetworkVariableFloat m_Health = new NetworkVariableFloat(new NetworkVariableSettings(){ SendTickrate = 0.0f, WritePermission = NetworkVariablePermission.ServerOnly } ,100.0f);

    /// <summary>
    /// m_MoveX
    /// Networked Var Use Case Scenario:  Player X-Axis Input
    /// Update Frequency: 100ms
    /// Used to send the player's X-Axis input value
    /// Everyone but the owner has read only access
    /// </summary>
    private NetworkVariableFloat m_MoveX = new NetworkVariableFloat(new NetworkVariableSettings(){ SendTickrate = 0.100f, WritePermission = NetworkVariablePermission.OwnerOnly } , 0);

    /// <summary>
    /// m_MoveY
    /// Networked Var Use Case Scenario:  Player Y-Axis Input
    /// Update Frequency: 100ms
    /// Used to send the player's Y-Axis input value
    /// Everyone but the owner has read only access
    /// </summary>
    private NetworkVariableFloat m_MoveY = new NetworkVariableFloat(new NetworkVariableSettings(){ SendTickrate = 0.100f, WritePermission = NetworkVariablePermission.OwnerOnly } , 0);

    /// <summary>
    /// m_MoveZ
    /// Networked Var Use Case Scenario:  Player Y-Axis Input
    /// Update Frequency: 100ms
    /// Used to send the player's Z-Axis input value
    /// Everyone but the owner has read only access
    /// </summary>
    private NetworkVariableFloat m_MoveZ = new NetworkVariableFloat(new NetworkVariableSettings(){ SendTickrate = 0.100f, WritePermission = NetworkVariablePermission.OwnerOnly } , 0);


    private bool m_HasGameStarted = false;

    bool IsAlive()
    {
        if (m_Health.Value <= 0.0f)
        {
            return false;
        }
        return true;
    }

    private void OnPositionChanged(Vector3 oldvalue, Vector3 newvalue)
    {
        transform.position = newvalue;
    }

    private SpriteRenderer m_PlayerVisual;

    private void Start()
    {
        m_PlayerVisual =  GetComponent<SpriteRenderer>();
        if (m_PlayerVisual != null)
        {
            m_PlayerVisual.material.color = Color.black;
        }
    }

    private GlobalGameState.GameStates m_CurrentGameState;
    private void GameStateChanged(GlobalGameState.GameStates previousState, GlobalGameState.GameStates newState)
    {
        m_CurrentGameState = newState;
        if (m_CurrentGameState == GlobalGameState.GameStates.InGame)
        {
            if (m_PlayerVisual != null)
            {
                m_PlayerVisual.material.color = Color.green;
            }
        }
        else
        {
            if (m_PlayerVisual != null)
            {
                m_PlayerVisual.material.color = Color.black;
            }
        }
    }

    public override void NetworkStart()
    {
        base.NetworkStart();

        m_Health.OnValueChanged += OnHealthChanged;

        GlobalGameState.Singleton.GameStateChanged += GameStateChanged;
    }

    /// <summary>
    /// OnHealthChanged
    /// </summary>
    /// <param name="previousAmount"></param>
    /// <param name="currentAmount"></param>
    protected void OnHealthChanged(float previousAmount, float currentAmount)
    {
        Debug.LogFormat("Health {0} ", currentAmount);
    }

    protected void OnDestroy()
    {
        GlobalGameState.Singleton.GameStateChanged -= GameStateChanged;
        m_Health.OnValueChanged -= OnHealthChanged;
    }

    /// <summary>
    /// GetLerpInputValue
    /// Helper method to lerp towards a target value while a specified key is pressed
    /// </summary>
    /// <param name="keyCode">key pressed</param>
    /// <param name="current">current input value</param>
    /// <param name="target">target input value (i.e. max or min depending upon sign)</param>
    /// <returns></returns>
    float GetLerpInputValue(KeyCode keyCode, float current, float target)
    {
        if (Input.GetKey(keyCode))
        {
            return  Mathf.Lerp(current, target, Time.deltaTime);
        }
        else
        {
           return Mathf.Lerp(current, 0.0f, Time.deltaTime);
        }
    }

    /// <summary>
    /// InGameUpdate
    /// Only updates when the game state is InGame
    /// </summary>
    private void InGameUpdate()
    {
        if (!IsAlive()) return;

        if (IsLocalPlayer)
        {
            m_MoveX.Value = GetLerpInputValue(KeyCode.LeftArrow, m_MoveX.Value, -1.0f);
            m_MoveX.Value = GetLerpInputValue(KeyCode.LeftArrow, m_MoveX.Value, 1.0f);
            //you could replicate this for each axis
        }

        if ( IsServer )
        {
            Vector3 newMovement = new Vector3(m_MoveX.Value,m_MoveY.Value,m_MoveZ.Value);
            transform.position = Vector3.MoveTowards(transform.position, transform.position + newMovement, m_MoveSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// MonoBehaviour.Update
    /// Process game state to determine what we should be doing
    /// </summary>
    void Update()
    {
        switch(m_CurrentGameState)
        {
            case GlobalGameState.GameStates.InGame:
                {
                    InGameUpdate();
                    break;
                }
            default:
                {
                    break;
                }
        }

    }

}
