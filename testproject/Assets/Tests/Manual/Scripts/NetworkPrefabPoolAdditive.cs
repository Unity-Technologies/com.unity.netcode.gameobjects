using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Spawning;

namespace TestProject.ManualTests
{
    public class NetworkPrefabPoolAdditive : NetworkBehaviour
    {
        [Header("General Settings")]
        public bool RandomMovement = true;
        public bool AutoSpawnEnable = true;
        public float InitialSpawnDelay;
        public int SpawnsPerSecond;
        public int PoolSize;
        public float ObjectSpeed = 10.0f;


        [Header("Prefab Instance Handling")]
        [Tooltip("When enabled, this will utilize the NetworkPrefabHandler to register a custom INetworkPrefabInstanceHandler")]
        public bool EnableHandler;

        [Tooltip("When enabled, this will register a custom INetworkPrefabInstanceHandler using a NetworkObject reference")]
        public bool RegisterUsingNetworkObject;

        [Tooltip("What is going to be spawned on the server side from the pool.")]
        public GameObject ServerObjectToPool;
        [Tooltip("What is going to be spawned on the client side from the ")]
        public GameObject ClientObjectToPool;

        private bool m_IsSpawningObjects;

        private float m_EntitiesPerFrame;
        private float m_DelaySpawning;

        private GameObject m_ObjectToSpawn;
        private List<GameObject> m_ObjectPool;

        private MyAdditiveCustomPrefabSpawnHandler m_AdditiveCustomPrefabSpawnHandler;

        /// <summary>
        /// Called when enabled, if already connected we register any custom prefab spawn handler here
        /// </summary>
        private void OnEnable()
        {
            //This registers early under the condition of a scene transition
            RegisterCustomPrefabHandler();
        }

        /// <summary>
        /// Handles registering the custom prefab handler
        /// </summary>
        private void RegisterCustomPrefabHandler()
        {
            // Register the custom spawn handler?
            if (m_AdditiveCustomPrefabSpawnHandler == null && EnableHandler)
            {
                if (NetworkManager && NetworkManager.PrefabHandler != null)
                {
                    m_AdditiveCustomPrefabSpawnHandler = new MyAdditiveCustomPrefabSpawnHandler(this);
                    if (RegisterUsingNetworkObject)
                    {
                        NetworkManager.PrefabHandler.AddHandler(ServerObjectToPool.GetComponent<NetworkObject>(), m_AdditiveCustomPrefabSpawnHandler);
                    }
                    else
                    {
                        NetworkManager.PrefabHandler.AddHandler(ServerObjectToPool, m_AdditiveCustomPrefabSpawnHandler);
                    }
                }
                else if (!IsServer)
                {
                    Debug.LogWarning($"Failed to register custom spawn handler and {nameof(EnableHandler)} is set to true!");
                }
            }
        }

        /// <summary>
        /// When disabled, stop spawning objects
        /// </summary>
        private void OnDisable()
        {
            m_IsSpawningObjects = false;
            if (NetworkManager.Singleton && EnableHandler && m_AdditiveCustomPrefabSpawnHandler != null)
            {
                var no = ServerObjectToPool.GetComponent<NetworkObject>();
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(no);
                m_AdditiveCustomPrefabSpawnHandler = null;
            }
        }

        /// <summary>
        /// General clean up
        /// The custom prefab handler is unregistered here
        /// </summary>
        private void OnDestroy()
        {
            if (NetworkManager != null && NetworkManager.SceneManager != null)
            {
                NetworkManager.SceneManager.OnSceneSwitchStarted -= SceneManager_OnSceneSwitchStarted;
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            SpawnsPerSecond = 3;
            NetworkManager.SceneManager.OnSceneSwitchStarted += SceneManager_OnSceneSwitchStarted;
            //Call this again in case we didn't have access to the NetworkManager already (i.e. first scene loaded)
            RegisterCustomPrefabHandler();

        }

        private void SceneManager_OnSceneSwitchStarted(AsyncOperation operation)
        {
            //OnSceneSwitchBegin();
        }

        /// <summary>
        /// Detect when we are switching scenes in order
        /// to assure we stop spawning objects
        /// </summary>
        private void OnSceneSwitchBegin()
        {
            if (IsServer)
            {
                StopCoroutine(SpawnObjects());

                if (m_ObjectPool != null)
                {
                    foreach (var obj in m_ObjectPool)
                    {
                        var networkObject = obj.GetComponent<NetworkObject>();
                        if (networkObject.IsSpawned)
                        {
                            networkObject.Despawn();
                        }
                        Destroy(obj);
                    }
                    m_ObjectPool.Clear();
                }
            }
        }

        /// <summary>
        /// Override NetworkBehaviour.NetworkStart
        /// </summary>
        public override void OnNetworkSpawn()
        {
            InitializeObjectPool();
            if (IsServer)
            {
                if (isActiveAndEnabled)
                {
                    m_DelaySpawning = Time.realtimeSinceStartup + InitialSpawnDelay;
                    StartSpawningBoxes();

                    //Make sure our slider reflects the current spawn rate
                    UpdateSpawnsPerSecond();
                }
            }
        }

        /// <summary>
        /// Determines which object is going to be spawned and then
        /// initializes the object pool based on
        /// </summary>
        public void InitializeObjectPool()
        {
            m_ObjectToSpawn = ServerObjectToPool;
            if (!IsServer && EnableHandler)
            {
                m_ObjectToSpawn = ClientObjectToPool;
            }

            if (IsServer)
            {
                m_ObjectPool = new List<GameObject>(PoolSize);

                for (int i = 0; i < PoolSize; i++)
                {
                    AddNewInstance();
                }
            }
        }

        /// <summary>
        /// Gets an object from the pool
        /// </summary>
        /// <returns></returns>
        public GameObject GetObject()
        {
            if (m_ObjectPool != null)
            {
                if (m_IsSpawningObjects)
                {
                    foreach (var obj in m_ObjectPool)
                    {
                        if (m_IsSpawningObjects)
                        {
                            if (!obj.activeInHierarchy)
                            {
                                obj.SetActive(true);
                                return obj;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    var newObj = AddNewInstance();
                    newObj.SetActive(true);
                    return newObj;
                }
            }
            return null;
        }

        /// <summary>
        /// Add a new instance to the object pool
        /// </summary>
        /// <returns>new instance of the m_ObjectToSpawn prefab</returns>
        private GameObject AddNewInstance()
        {
            var obj = Instantiate(m_ObjectToSpawn);
            var no = obj.GetComponent<NetworkObject>();
            var genericBehaviour = obj.GetComponent<GenericNetworkObjectBehaviour>();
            if(genericBehaviour)
            {
                genericBehaviour.ShouldMoveRandomly(RandomMovement);
            }
            obj.SetActive(false);


            m_ObjectPool.Add(obj);
            return obj;
        }

        /// <summary>
        /// Starts the
        /// </summary>
        private void StartSpawningBoxes()
        {
            if (SpawnsPerSecond > 0)
            {
                StartCoroutine(SpawnObjects());
            }
        }

        /// <summary>
        /// Checks to determine if we need to update our spawns per second calculations
        /// </summary>
        public void UpdateSpawnsPerSecond()
        {
            // Handle case where the initial value is set to zero and so coroutine needs to be started
            if(SpawnsPerSecond > 0 && !m_IsSpawningObjects)
            {
                StartSpawningBoxes();
            }
            else //Handle case where spawning coroutine is running but we set our spawn rate to zero
            if (SpawnsPerSecond == 0 && m_IsSpawningObjects)
            {
                m_IsSpawningObjects = false;
                StopCoroutine(SpawnObjects());
            }

        }

        /// <summary>
        /// Coroutine to spawn boxes
        /// </summary>
        /// <returns></returns>
        private IEnumerator SpawnObjects()
        {
            //Exit if we are a client or we happen to not have a NetworkManager
            if (NetworkManager == null || (NetworkManager.IsClient && !NetworkManager.IsHost && !NetworkManager.IsServer))
            {
                yield return null;
            }

            if (m_DelaySpawning > Time.realtimeSinceStartup)
            {
                yield return new WaitForSeconds(m_DelaySpawning - Time.realtimeSinceStartup);
            }

            m_IsSpawningObjects = true;
            while (m_IsSpawningObjects)
            {
                //Start spawning if auto spawn is enabled
                if (AutoSpawnEnable)
                {
                    float entitySpawnUpdateRate = 1.0f;
                    if (SpawnsPerSecond > 0)
                    {
                        entitySpawnUpdateRate = 1.0f / Mathf.Min(SpawnsPerSecond, 60.0f);
                        //While not 100% accurate, this basically allows for higher entities per second generation
                        m_EntitiesPerFrame = (float)SpawnsPerSecond * entitySpawnUpdateRate;
                        int entitityCountPerFrame = Mathf.RoundToInt(m_EntitiesPerFrame);
                        //Spawn (n) entities then wait for 1/60th of a second and repeat
                        for (int i = 0; i < entitityCountPerFrame; i++)
                        {
                            GameObject go = GetObject();
                            if (go != null)
                            {
                                go.transform.position = transform.position;

                                float ang = Random.Range(0.0f, 2 * Mathf.PI);
                                go.GetComponent<GenericNetworkObjectBehaviour>().SetDirectionAndVelocity(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)), ObjectSpeed);

                                var no = go.GetComponent<NetworkObject>();
                                if (!no.IsSpawned)
                                {
                                    no.Spawn(null, true);
                                }
                            }
                        }
                    }
                    yield return new WaitForSeconds(entitySpawnUpdateRate);
                }
                else //Just hang out until it is enabled
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }
    }


    /// <summary>
    /// The custom prefab handler that returns an object from the prefab pool
    /// </summary>
    public class MyAdditiveCustomPrefabSpawnHandler : INetworkPrefabInstanceHandler
    {
        private NetworkPrefabPoolAdditive m_PrefabPool;
        public NetworkObject HandleNetworkPrefabSpawn(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var obj = m_PrefabPool.GetObject();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                return obj.GetComponent<NetworkObject>();
            }
            return null;
        }
        public void HandleNetworkPrefabDestroy(NetworkObject networkObject)
        {
            networkObject.transform.position = Vector3.zero;
            networkObject.gameObject.SetActive(false);
        }

        public MyAdditiveCustomPrefabSpawnHandler(NetworkPrefabPoolAdditive objectPool)
        {
            m_PrefabPool = objectPool;
        }
    }
}
