using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class RandomPlayerMover : NetworkBehaviour
{
    [Tooltip("How fast the player will move.")]
    [Range(0.5f,20.0f)]
    [SerializeField]
    private float m_MoveSpeed = 5;

    [Tooltip("How fast the camera will rotate towards the curent player's movement direction.")]
    [Range(1.0f,5.0f)]
    [SerializeField]
    private float m_LookSpeed = 0.25f;

    [Tooltip("A transform in the player prefab that already has the view position and rotation to keep the main cmaera aligned.")]
    [SerializeField]
    private GameObject m_CameraRoot;

    [HideInInspector]
    [SerializeField]
    private RigidbodyConstraints m_DefaultConstraints;

    [SerializeField]
    private Vector3 m_PlayerStartOffset;

    [SerializeField]
    private GameObject m_PlayerSpawnPoints;


    private bool m_IsPaused;                //Determines if we are paused or not
    private Vector3 m_Direction;            //Determines what direction we will move towards
    private List<Transform> m_SpawnPoints;  //Spawn points to randomly pick from when starting

    private MeshRenderer m_MeshRenderer;    //The visual component for the PlayerObject -- use for hiding or showing the players
    private Rigidbody m_Rigidbody;          //The rigid body for the PlayerObject (only for collision!!!!)

    private InGameManager m_InGameManager;  //The m_InGameManager (!! only set when in the InGame scene !!)

    private SyncTransform m_NetworkTransform;

    private static Color[] s_PlayerColors = { Color.red, Color.yellow, Color.green, Color.blue, Color.cyan, Color.magenta, Color.white };

    /// <summary>
    /// Sets the player color based on its id;
    /// </summary>
    void SetPlayerColor()
    {
        if (m_MeshRenderer)
        {
            m_MeshRenderer.material.color = s_PlayerColors[NetworkObject.OwnerClientId % System.Convert.ToUInt64(s_PlayerColors.Length)];
        }
    }

    /// <summary>
    /// Initialize the PlayerObject's random mover component
    /// </summary>
    private void Start()
    {
        m_NetworkTransform = GetComponent<SyncTransform>();

        m_Rigidbody = GetComponent<Rigidbody>();
        if (m_Rigidbody)
        {
            if (m_DefaultConstraints == RigidbodyConstraints.None)
            {
                m_DefaultConstraints = m_Rigidbody.constraints;
            }
            else
            {
                m_Rigidbody.constraints = m_DefaultConstraints;
            }
        }

        m_MeshRenderer = GetComponent<MeshRenderer>();

        if (IsOwner)
        {
            SetPlayerSpawnPoint();
            m_Direction = new Vector3(Random.Range(-100.0f, 100.0f), 0, Random.Range(-100.0f, 100.0f));
            m_Direction.Normalize();
        }

        //All instances register to this in order to change their state according to the global game state
        GlobalGameState.Singleton.GameStateChanged += GlobalGameStateChanged;
        GlobalGameState.Singleton.ClientLoadedScene += ClientLoadedScene;

        SetPlayerColor();
    }

    /// <summary>
    /// Invoked upon each scene load
    /// </summary>
    /// <param name="clientId"></param>
    private void ClientLoadedScene(ulong clientId)
    {
        if(clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetPlayerSpawnPoint();
        }
    }

    /// <summary>
    /// This is one (of many) common ways to handle spawn points.
    /// ** Note: It still could end up in potential collision as each individual player is picking their own start location
    /// ** This could be improved upon by extending this to the server and having the server preselect spawn points for each player.
    /// </summary>
    void SetPlayerSpawnPoint()
    {
        if (m_PlayerSpawnPoints != null)
        {
            m_SpawnPoints = new List<Transform>(m_PlayerSpawnPoints.GetComponentsInChildren<Transform>());
        }
        if (m_SpawnPoints != null && m_SpawnPoints.Count > 0)
        {
            transform.position = m_SpawnPoints[Random.Range(0, m_SpawnPoints.Count - 1)].position;
        }
        else
        {
            transform.position = m_PlayerStartOffset;
        }
    }

    /// <summary>
    /// One way to handle setting the default camera to a player is to add a "Root Transform" (empty game object)
    /// as a child on the player prefab (PlayerObject). Using this method, you can set the camera's transform's
    /// parent to be the m_CameraRoot.transform
    /// </summary>
    public void SetPlayerCamera(bool isActive = true)
    {
        if (IsLocalPlayer)
        {
            Camera camera = m_CameraRoot.GetComponentInChildren<Camera>();
            if (camera)
            {
                camera.enabled = isActive;
            }
        }
    }

    /// <summary>
    /// Example of using the global game state to enable or disable locally
    /// This does not handle the clone's.
    /// TODO: Created a networked camera that handles all of this automatically
    /// </summary>
    /// <param name="previousState"></param>
    /// <param name="newState"></param>
    private void GlobalGameStateChanged(GlobalGameState.GameStates previousState, GlobalGameState.GameStates newState)
    {
        HandlGlobalGameStateChanged(previousState, false);
        HandlGlobalGameStateChanged(newState, true);
    }

    /// <summary>
    /// We use this pattern with handling state or value changes.
    /// With state changes it is useful to easily enable or disable something based on the GlobalGameState
    /// </summary>
    /// <param name="globalGameState"></param>
    /// <param name="isTransitioningTo"></param>
    void HandlGlobalGameStateChanged(GlobalGameState.GameStates globalGameState, bool isTransitioningTo)
    {
        switch (globalGameState)
        {
            case GlobalGameState.GameStates.Lobby:
                {
                    OnIsHidden(isTransitioningTo);
                    break;
                }
            case GlobalGameState.GameStates.InGame:
                {
                    break;
                }
        }
    }

    /// <summary>
    /// The InGameManager invokes this so the component can react to In-Game state changes
    /// </summary>
    /// <param name="inGameManager"></param>
    public void OnRegisterInGameManager(InGameManager inGameManager)
    {
        if (m_InGameManager != null && m_InGameManager != inGameManager)
        {
            m_InGameManager.OnInGameStateChanged -= OnInGameStateChanged;
        }
        m_InGameManager = inGameManager;
        m_InGameManager.OnInGameStateChanged += OnInGameStateChanged;
    }

    /// <summary>
    /// Will be notified of the In-Game state transition (from -> to)
    /// </summary>
    /// <param name="previousState">From</param>
    /// <param name="newState">To</param>
    private void OnInGameStateChanged(InGameManager.InGameStates previousState, InGameManager.InGameStates newState)
    {
        HandleInGameStateChanged(previousState, false);
        HandleInGameStateChanged(newState, true);
    }

    /// <summary>
    /// Same pattern is used as was used with the GlobalGameState GameState changes.
    /// </summary>
    /// <param name="gameState">state</param>
    /// <param name="isTransitioningTo">are we transitioning to or from this state?</param>
    void HandleInGameStateChanged(InGameManager.InGameStates gameState, bool isTransitioningTo)
    {
        switch (gameState)
        {
            case InGameManager.InGameStates.Exiting:
            case InGameManager.InGameStates.Paused:
                {
                    OnPaused(isTransitioningTo);
                    if (m_NetworkTransform != null)
                    {
                        m_NetworkTransform.enabled = !isTransitioningTo;
                    }
                    break;
                }
        }
    }

    /// <summary>
    /// Hides the rendering component of this game object
    /// Note: this is specific to MeshRenderer
    /// </summary>
    /// <param name="isHidden">are we hidden?</param>
    public void OnIsHidden(bool isHidden)
    {
        if (!m_MeshRenderer)
        {
            m_MeshRenderer = GetComponent<MeshRenderer>();
        }

        if (m_MeshRenderer)
        {
            m_MeshRenderer.enabled = !isHidden;
        }
    }

    /// <summary>
    /// Invoked when the server hits pause
    /// </summary>
    /// <param name="isPaused"></param>
    public void OnPaused(bool isPaused)
    {
        if (m_IsPaused != isPaused)
        {
            m_IsPaused = isPaused;

            //This just assures we have the rigid body
            if (!m_Rigidbody)
            {
                m_Rigidbody = GetComponent<Rigidbody>();
                if (m_Rigidbody)
                {
                    if (m_DefaultConstraints == RigidbodyConstraints.None)
                    {
                        m_DefaultConstraints = m_Rigidbody.constraints;
                    }
                    else
                    {
                        m_Rigidbody.constraints = m_DefaultConstraints;
                    }
                }
            }

            if (m_Rigidbody)
            {
                if (m_IsPaused)
                {
                    m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                    m_Rigidbody.velocity = Vector3.zero;
                    m_Rigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    m_Rigidbody.constraints = m_DefaultConstraints;

                }
            }
        }
    }

    /// <summary>
    /// Moves the player in the current direction (m_Direction) at a given speed
    /// </summary>
    /// <param name="speed"></param>
    public void Move(float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, transform.position + m_Direction * (speed * Time.fixedDeltaTime), speed * Time.fixedDeltaTime);
        Vector3 LookDir = Vector3.Lerp(transform.forward, m_Direction, m_LookSpeed * Time.fixedDeltaTime);
        transform.rotation = Quaternion.LookRotation(LookDir);
    }

    /// <summary>
    /// Updates the movement for the player during physics fixed update
    /// </summary>
    private void FixedUpdate()
    {
        if (!m_IsPaused)
        {
            if (IsOwner)
            {
                Move(m_MoveSpeed);
            }
        }
    }

    /// <summary>
    /// When we collide with something other than the ground, change our direction
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionStay(Collision collision)
    {
        //Only owners should handle change in direction based on collision
        if (IsOwner)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                return;
            }

            if (m_SpawnPoints != null && m_SpawnPoints.Count > 0)
            {
                m_Direction = m_SpawnPoints[Random.Range(0, m_SpawnPoints.Count - 1)].position - transform.position;
                m_Direction.Normalize();
            }
            else  //Handle the case where there are no spawn points
            {
                List<ContactPoint> contactPoints = new List<ContactPoint>(collision.contactCount);
                if (collision.GetContacts(contactPoints) > 0)
                {
                    Vector3 CollisionPointAverage = Vector3.zero;
                    foreach (ContactPoint contactPoint in contactPoints)
                    {
                        CollisionPointAverage += contactPoint.point;
                    }
                    CollisionPointAverage *= 1.0f / (float)collision.contactCount;
                    Vector3 MoveAway = this.transform.position - CollisionPointAverage;
                    MoveAway.Normalize();
                    m_Direction = MoveAway;
                }
            }
        }
    }
}
