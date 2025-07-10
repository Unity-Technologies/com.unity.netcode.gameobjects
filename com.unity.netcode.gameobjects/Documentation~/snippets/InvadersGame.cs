using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public enum InvadersObjectType
{
    Enemy = 1,
    Shield,
    Max
}

[Flags]
public enum UpdateEnemiesResultFlags : byte
{
    None = 0x0000,
    FoundEnemy = 0x0001, // Found at least one eligible enemy to continue, without creating a new set
    ReachedHorizontalBoundary = 0x0002, // If at least one of the enemies reached either left or right boundary
    ReachedBottom = 0x004, // If at least one of the enemies reached the bottom boundary the game is over
    Max
}

public enum GameOverReason : byte
{
    None = 0,
    EnemiesReachedBottom = 1,
    Death = 2,
    Max,
}

public class InvadersGame : NetworkBehaviour
{
    // The vertical offset we apply to each Enemy transform once they touch an edge
    private const float k_EnemyVerticalMovementOffset = -0.8f;
    private const float k_LeftOrRightBoundaryOffset = 10.0f;
    private const float k_BottomBoundaryOffset = 1.25f;

    [Header("Prefab settings")]
    public GameObject enemy1Prefab;
    public GameObject enemy2Prefab;
    public GameObject enemy3Prefab;
    public GameObject superEnemyPrefab;
    public GameObject shieldPrefab;

    [Header("UI Settings")]
    public TMP_Text gameTimerText;
    public TMP_Text scoreText;
    public TMP_Text livesText;
    public TMP_Text gameOverText;

    [Header("GameMode Settings")]
    public Transform saucerSpawnPoint;

    [SerializeField]
    [Tooltip("Time Remaining until the game starts")]
    private float m_DelayedStartTime = 5.0f;

    [SerializeField]
    private NetworkVariable<float> m_TickPeriodic = new NetworkVariable<float>(0.2f);

    [SerializeField]
    private NetworkVariable<float> m_EnemyMovingDirection = new NetworkVariable<float>(0.3f);

    [SerializeField]
    private float m_RandomThresholdForSaucerCreation = 0.92f;

    private List<EnemyAgent> m_Enemies = new List<EnemyAgent>();

    //These help to simplify checking server vs client
    //[NSS]: This would also be a great place to add a state machine and use networked vars for this
    private bool m_ClientGameOver;
    private bool m_ClientGameStarted;
    private bool m_ClientStartCountdown;

    private NetworkVariable<bool> m_CountdownStarted = new NetworkVariable<bool>(false);

    private float m_NextTick;

    // the timer should only be synced at the beginning
    // and then let the client to update it in a predictive manner
    private bool m_ReplicatedTimeSent = false;
    private GameObject m_Saucer;
    private List<Shield> m_Shields = new List<Shield>();
    private float m_TimeRemaining;

    public static InvadersGame Singleton { get; private set; }

    public NetworkVariable<bool> hasGameStarted { get; } = new NetworkVariable<bool>(false);

    public NetworkVariable<bool> isGameOver { get; } = new NetworkVariable<bool>(false);

    /// <summary>
    ///     Awake
    ///     A good time to initialize server side values
    /// </summary>
    private void Awake()
    {
        Assert.IsNull(Singleton, $"Multiple instances of {nameof(InvadersGame)} detected. This should not happen.");
        Singleton = this;
        
        OnSingletonReady?.Invoke();

        if (IsServer)
        {
            hasGameStarted.Value = false;

            //Set our time remaining locally
            m_TimeRemaining = m_DelayedStartTime;

            //Set for server side
            m_ReplicatedTimeSent = false;
        }
        else
        {
            //We do a check for the client side value upon instantiating the class (should be zero)
            Debug.LogFormat("Client side we started with a timer value of {0}", m_TimeRemaining);
        }
    }

    /// <summary>
    ///     Update
    ///     MonoBehaviour Update method
    /// </summary>
    private void Update()
    {
        //Is the game over?
        if (IsCurrentGameOver()) return;

        //Update game timer (if the game hasn't started)
        UpdateGameTimer();

        //If we are a connected client, then don't update the enemies (server side only)
        if (!IsServer) return;

        //If we are the server and the game has started, then update the enemies
        if (HasGameStarted()) UpdateEnemies();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
        {
            m_Enemies.Clear();
            m_Shields.Clear();
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    internal static event Action OnSingletonReady;

    public override void OnNetworkSpawn()
    {
        if (IsClient && !IsServer)
        {
            m_ClientGameOver = false;
            m_ClientStartCountdown = false;
            m_ClientGameStarted = false;

            m_CountdownStarted.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientStartCountdown = newValue;
                Debug.LogFormat("Client side we were notified the start count down state was {0}", newValue);
            };

            hasGameStarted.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientGameStarted = newValue;
                gameTimerText.gameObject.SetActive(!m_ClientGameStarted);
                Debug.LogFormat("Client side we were notified the game started state was {0}", newValue);
            };

            isGameOver.OnValueChanged += (oldValue, newValue) =>
            {
                m_ClientGameOver = newValue;
                Debug.LogFormat("Client side we were notified the game over state was {0}", newValue);
            };
        }

        //Both client and host/server will set the scene state to "ingame" which places the PlayerControl into the SceneTransitionHandler.SceneStates.INGAME
        //and in turn makes the players visible and allows for the players to be controlled.
        SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Ingame);

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        base.OnNetworkSpawn();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (m_ReplicatedTimeSent)
        {
            // Send the RPC only to the newly connected client
            SetReplicatedTimeRemainingClientRPC(m_TimeRemaining, new ClientRpcParams {Send = new ClientRpcSendParams{TargetClientIds = new List<ulong>() {clientId}}});
        }
    }

    /// <summary>
    ///     ShouldStartCountDown
    ///     Determines when the countdown should start
    /// </summary>
    /// <returns>true or false</returns>
    private bool ShouldStartCountDown()
    {
        //If the game has started, then don't bother with the rest of the count down checks.
        if (HasGameStarted()) return false;
        if (IsServer)
        {
            m_CountdownStarted.Value = SceneTransitionHandler.sceneTransitionHandler.AllClientsAreLoaded();

            //While we are counting down, continually set the replicated time remaining value for clients (client should only receive the update once)
            if (m_CountdownStarted.Value && !m_ReplicatedTimeSent)
            {
                SetReplicatedTimeRemainingClientRPC(m_DelayedStartTime);
                m_ReplicatedTimeSent = true;
            }

            return m_CountdownStarted.Value;
        }

        return m_ClientStartCountdown;
    }

    /// <summary>
    ///     We want to send only once the Time Remaining so the clients
    ///     will deal with updating it. For that, we use a ClientRPC
    /// </summary>
    /// <param name="delayedStartTime"></param>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    private void SetReplicatedTimeRemainingClientRPC(float delayedStartTime, ClientRpcParams clientRpcParams = new ClientRpcParams())
    {
        // See the ShouldStartCountDown method for when the server updates the value
        if (m_TimeRemaining == 0)
        {
            Debug.LogFormat("Client side our first timer update value is {0}", delayedStartTime);
            m_TimeRemaining = delayedStartTime;
        }
        else
        {
            Debug.LogFormat("Client side we got an update for a timer value of {0} when we shouldn't", delayedStartTime);
        }
    }

    /// <summary>
    ///     IsCurrentGameOver
    ///     Returns whether the game is over or not
    /// </summary>
    /// <returns>true or false</returns>
    private bool IsCurrentGameOver()
    {
        if (IsServer)
            return isGameOver.Value;
        return m_ClientGameOver;
    }

    /// <summary>
    ///     HasGameStarted
    ///     Determine whether the game has started or not
    /// </summary>
    /// <returns>true or false</returns>
    private bool HasGameStarted()
    {
        if (IsServer)
            return hasGameStarted.Value;
        return m_ClientGameStarted;
    }

    /// <summary>
    ///     Client side we try to predictively update the gameTimer
    ///     as there shouldn't be a need to receive another update from the server
    ///     We only got the right m_TimeRemaining value when we started so it will be enough
    /// </summary>
    /// <returns> True when m_HasGameStared is set </returns>
    private void UpdateGameTimer()
    {
        if (!ShouldStartCountDown()) return;
        if (!HasGameStarted() && m_TimeRemaining > 0.0f)
        {
            m_TimeRemaining -= Time.deltaTime;

            if (IsServer && m_TimeRemaining <= 0.0f) // Only the server should be updating this
            {
                m_TimeRemaining = 0.0f;
                hasGameStarted.Value = true;
                OnGameStarted();
            }

            if (m_TimeRemaining > 0.1f)
                gameTimerText.SetText("{0}", Mathf.FloorToInt(m_TimeRemaining));
        }
    }

    /// <summary>
    ///     OnGameStarted
    ///     Only invoked by the server, this hides the timer text and initializes the enemies and level
    /// </summary>
    private void OnGameStarted()
    {
        gameTimerText.gameObject.SetActive(false);
        CreateEnemies();
        CreateShields();
        CreateSuperEnemy();
    }

    private void UpdateEnemies()
    {
        // Update enemies
        if (Time.time >= m_NextTick)
        {
            m_NextTick = Time.time + m_TickPeriodic.Value;

            UpdateEnemiesResultFlags enemiesResultFlags = UpdateEnemiesResultFlags.None;
            UpdateShootingEnemies(ref enemiesResultFlags);

            if((enemiesResultFlags & UpdateEnemiesResultFlags.ReachedBottom) != 0)
            {
                // Force game end as at least one of the enemies have reached the bottom!
                SetGameEnd(GameOverReason.EnemiesReachedBottom);
                return;
            }
            
            // If we didn't find any enemies, then spawn some
            if ((enemiesResultFlags & UpdateEnemiesResultFlags.FoundEnemy) == 0)
            {
                CreateEnemies();
                m_TickPeriodic.Value = 0.2f;
            }

            // If the enemies reached the either side of the boundaries, then change the movement direction
            // And move them to the next row below
            if ((enemiesResultFlags & UpdateEnemiesResultFlags.ReachedHorizontalBoundary) != 0)
            {
                m_EnemyMovingDirection.Value = -m_EnemyMovingDirection.Value;
                m_TickPeriodic.Value *= 0.95f; // get faster

                var enemiesCount = m_Enemies.Count;
                for (var index = 0; index < enemiesCount; index++)
                {
                    var enemy = m_Enemies[index];
                    enemy.transform.Translate(0, k_EnemyVerticalMovementOffset, 0);
                }
            }

            if (m_Saucer == null)
                if (Random.Range(0, 1.0f) > m_RandomThresholdForSaucerCreation)
                    CreateSuperEnemy();
        }
    }

    private void UpdateShootingEnemies(ref UpdateEnemiesResultFlags flags)
    {
        flags = UpdateEnemiesResultFlags.None;
        var enemiesCount = m_Enemies.Count;
        for (var index = 0; index < enemiesCount; index++)
        {
            var enemy = m_Enemies[index];
            Assert.IsNotNull(enemy);
            if (m_Enemies == null)
            {
                continue;
            }
            
            // If at least one of the enemies reached bottom, return early.
            if (enemy.transform.position.y <= k_BottomBoundaryOffset)
            {
                flags |= UpdateEnemiesResultFlags.ReachedBottom;
                return;
            }
            
            if (enemy.score > 100)
                continue;

            flags |= UpdateEnemiesResultFlags.FoundEnemy;
            enemy.transform.position += new Vector3(m_EnemyMovingDirection.Value, 0, 0);

            if (enemy.transform.position.x > k_LeftOrRightBoundaryOffset || enemy.transform.position.x < -k_LeftOrRightBoundaryOffset)
                flags |= UpdateEnemiesResultFlags.ReachedHorizontalBoundary;

            // can shoot if the lowest in my column
            var canShoot = true;
            var column = enemy.column;
            var row = enemy.row;
            for (var otherIndex = 0; otherIndex < enemiesCount; otherIndex++)
            {
                var otherEnemy = m_Enemies[otherIndex];
                Assert.IsTrue(otherEnemy != null);

                if (Math.Abs(otherEnemy.column - column) < 0.001f)
                    if (otherEnemy.row < row)
                    {
                        canShoot = false;
                        break;
                    }
            }

            enemy.canShoot = canShoot;
        }
    }

    public void SetScore(int score)
    {
        scoreText.SetText("0{0}", score);
    }

    public void SetLives(int lives)
    {
        livesText.SetText("0{0}", lives);
    }

    public void DisplayGameOverText(string message)
    {
        if (gameOverText)
        {
            gameOverText.SetText(message);
            gameOverText.gameObject.SetActive(true);
        }
    }

    public void SetGameEnd(GameOverReason reason)
    {
        Assert.IsTrue(IsServer, "SetGameEnd should only be called server side!");

        // We should only end the game if all the player's are dead
        if (reason != GameOverReason.Death)
        {
            this.isGameOver.Value = true;
            BroadcastGameOverClientRpc(reason); // Notify our clients!
            return;
        }
        
        foreach (NetworkClient networkedClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = networkedClient.PlayerObject;
            if(playerObject == null) continue;
            
            // We should just early out if any of the player's are still alive
            if (playerObject.GetComponent<PlayerControl>().IsAlive)
                return;
        }
        
        this.isGameOver.Value = true;
    }

    [ClientRpc]
    public void BroadcastGameOverClientRpc(GameOverReason reason)
    {
        var localPlayerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        Assert.IsNotNull(localPlayerObject);

        if (localPlayerObject.TryGetComponent<PlayerControl>(out var playerControl))
            playerControl.NotifyGameOver(reason);
    }

    public void RegisterSpawnableObject(InvadersObjectType invadersObjectType, GameObject gameObject)
    {
        Assert.IsTrue(IsClient);

        switch (invadersObjectType)
        {
            case InvadersObjectType.Enemy:
            {
                // Don't register if this is a saucer
                if (gameObject.TryGetComponent<SuperEnemyMovement>(out var saucer))
                    return;

                gameObject.TryGetComponent<EnemyAgent>(out var enemyAgent);
                Assert.IsTrue(enemyAgent != null);
                if (!m_Enemies.Contains(enemyAgent))
                    m_Enemies.Add(enemyAgent);
                break;
            }
            case InvadersObjectType.Shield:
            {
                gameObject.TryGetComponent<Shield>(out var shield);
                Assert.IsTrue(shield != null);
                m_Shields.Add(shield);
                break;
            }
            default:
                Assert.IsTrue(false);
                break;
        }
    }

    public void UnregisterSpawnableObject(InvadersObjectType invadersObjectType, GameObject gameObject)
    {
        Assert.IsTrue(IsServer);

        switch (invadersObjectType)
        {
            case InvadersObjectType.Enemy:
            {
                // Don't unregister if this is a saucer
                if (gameObject.TryGetComponent<SuperEnemyMovement>(out var saucer))
                    return;

                gameObject.TryGetComponent<EnemyAgent>(out var enemyAgent);
                Assert.IsTrue(enemyAgent != null);
                if (m_Enemies.Contains(enemyAgent))
                    m_Enemies.Remove(enemyAgent);
                break;
            }
            case InvadersObjectType.Shield:
            {
                gameObject.TryGetComponent<Shield>(out var shield);
                Assert.IsTrue(shield != null);
                if (m_Shields.Contains(shield))
                    m_Shields.Remove(shield);
                break;
            }
            default:
                Assert.IsTrue(false);
                break;
        }
    }

    public void ExitGame()
    {
        NetworkManager.Singleton.Shutdown();
        SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
    }

    private void CreateShield(GameObject prefab, int posX, int posY)
    {
        Assert.IsTrue(IsServer, "Create Shield should be called server-side only!");

        const float dy = 0.41f;
        const float dx = 0.41f;

        var ycount = 0;
        for (float y = posY; y < posY + 2; y += dy)
        {
            var xcount = 0;
            for (float x = posX; x < posX + 2; x += dx)
            {
                if (ycount == 4 && (xcount == 0 || xcount == 4))
                {
                    xcount += 1;
                    continue;
                }

                var shield = Instantiate(prefab, new Vector3(x - 1, y, 0), Quaternion.identity);

                // Spawn the Networked Object, this should notify the clients
                shield.GetComponent<NetworkObject>().Spawn();
                xcount += 1;
            }

            ycount += 1;
        }
    }

    private void CreateShields()
    {
        // Create Shields
        CreateShield(shieldPrefab, -7, -1);
        CreateShield(shieldPrefab, 0, -1);
        CreateShield(shieldPrefab, 7, -1);
    }

    private void CreateSuperEnemy()
    {
        Assert.IsTrue(IsServer, "Create Saucer should be called server-side only!");

        m_Saucer = Instantiate(superEnemyPrefab, saucerSpawnPoint.position, Quaternion.identity);

        // Spawn the Networked Object, this should notify the clients
        m_Saucer.GetComponent<NetworkObject>().Spawn();
    }

    private void CreateEnemy(GameObject prefab, float posX, float posY)
    {
        Assert.IsTrue(IsServer, "Create Enemy should be called server-side only!");

        var enemy = Instantiate(prefab);
        enemy.transform.position = new Vector3(posX, posY, 0.0f);
        enemy.GetComponent<EnemyAgent>().Setup(Mathf.RoundToInt(posX), Mathf.RoundToInt(posY));

        // Spawn the Networked Object, this should notify the clients
        enemy.GetComponent<NetworkObject>().Spawn();
    }

    public void CreateEnemies()
    {
        float startx = -8;
        for (var i = 0; i < 10; i++)
        {
            CreateEnemy(enemy1Prefab, startx, 12);
            startx += 1.6f;
        }

        startx = -8;
        for (var i = 0; i < 10; i++)
        {
            CreateEnemy(enemy2Prefab, startx, 10);
            startx += 1.6f;
        }

        startx = -8;
        for (var i = 0; i < 10; i++)
        {
            CreateEnemy(enemy3Prefab, startx, 8);
            startx += 1.6f;
        }
    }
}
