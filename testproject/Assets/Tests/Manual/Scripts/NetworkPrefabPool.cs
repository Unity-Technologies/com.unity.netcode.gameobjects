using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace TestProject.ManualTests
{
    public class NetworkPrefabPool : NetworkBehaviour
    {
        [Header("General Settings")]
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

        [Header("Component Connections")]
        public SwitchSceneHandler SwitchScene;
        public Slider SpawnSlider;
        public Text SpawnSliderValueText;

        private bool m_IsSpawningObjects;

        private float m_EntitiesPerFrame;
        private float m_DelaySpawning;

        private GameObject m_ObjectToSpawn;
        private List<GameObject> m_ObjectPool;

        private MyCustomPrefabSpawnHandler m_MyCustomPrefabSpawnHandler;

        /// <summary>
        /// Called when enabled, if already connected we register any custom prefab spawn handler here
        /// </summary>
        private void OnEnable()
        {
            if (SpawnSlider != null)
            {
                SpawnSlider.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Handles registering the custom prefab handler
        /// </summary>
        private void RegisterCustomPrefabHandler()
        {
            // Register the custom spawn handler?
            if (m_MyCustomPrefabSpawnHandler == null && EnableHandler)
            {
                if (NetworkManager && NetworkManager.PrefabHandler != null)
                {
                    m_MyCustomPrefabSpawnHandler = new MyCustomPrefabSpawnHandler(this);
                    if (RegisterUsingNetworkObject)
                    {
                        NetworkManager.PrefabHandler.AddHandler(ServerObjectToPool.GetComponent<NetworkObject>(), m_MyCustomPrefabSpawnHandler);
                    }
                    else
                    {
                        NetworkManager.PrefabHandler.AddHandler(ServerObjectToPool, m_MyCustomPrefabSpawnHandler);
                    }
                }
                else if (!IsServer)
                {
                    Debug.LogWarning($"Failed to register custom spawn handler and {nameof(EnableHandler)} is set to true!");
                }
            }
        }


        private void DeregisterCustomPrefabHandler()
        {
            if (EnableHandler && NetworkManager && NetworkManager.PrefabHandler != null && m_MyCustomPrefabSpawnHandler != null)
            {
                NetworkManager.PrefabHandler.RemoveHandler(ServerObjectToPool);
                if (IsClient && m_ObjectToSpawn != null)
                {
                    NetworkManager.PrefabHandler.RemoveHandler(m_ObjectToSpawn);
                }
            }
        }



        /// <summary>
        /// When disabled, stop spawning objects
        /// </summary>
        private void OnDisable()
        {
            m_IsSpawningObjects = false;
            if (NetworkManager.Singleton && EnableHandler && m_MyCustomPrefabSpawnHandler != null)
            {
                var no = ServerObjectToPool.GetComponent<NetworkObject>();
                NetworkManager.Singleton.PrefabHandler.RemoveHandler(no);
                m_MyCustomPrefabSpawnHandler = null;
            }


        }

        /// <summary>
        /// General clean up
        /// The custom prefab handler is de-registered here
        /// </summary>
        private void OnDestroy()
        {
            if (SwitchScene)
            {
                SwitchScene.OnSceneSwitchBegin -= OnSceneSwitchBegin;
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            if (SpawnSliderValueText != null)
            {
                SpawnSliderValueText.text = SpawnsPerSecond.ToString();
            }

            if (SwitchScene)
            {
                SwitchScene.OnSceneSwitchBegin += OnSceneSwitchBegin;
            }
        }

        /// <summary>
        /// Detect when we are switching scenes in order
        /// to assure we stop spawning objects
        /// </summary>
        private void OnSceneSwitchBegin()
        {
            DeregisterCustomPrefabHandler();
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
                            networkObject.Despawn(true);
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
            // Base construction and registration of the custom prefab handler.
            RegisterCustomPrefabHandler();

            // Default to the server side object
            m_ObjectToSpawn = ServerObjectToPool;

            // Host and Client need to do an extra step
            if (IsClient)
            {
                if (EnableHandler && ClientObjectToPool != null)
                {
                    m_ObjectToSpawn = NetworkManager.GetNetworkPrefabOverride(ClientObjectToPool);
                }
                else
                {
                    m_ObjectToSpawn = NetworkManager.GetNetworkPrefabOverride(m_ObjectToSpawn);
                }

                // Since the host should spawn the override, we need to register the host to link it to the originally registered ServerObjectToPool
                if (IsHost && EnableHandler && ServerObjectToPool != m_ObjectToSpawn)
                {
                    // While this seems redundant, we could theoretically have several objects that we could potentially be spawning
                    NetworkManager.PrefabHandler.RegisterHostGlobalObjectIdHashValues(ServerObjectToPool, new List<GameObject>() { m_ObjectToSpawn });
                }
            }

            if (EnableHandler || IsServer)
            {
                m_ObjectPool = new List<GameObject>(PoolSize);

                for (int i = 0; i < PoolSize; i++)
                {
                    var gameObject = AddNewInstance();
                    gameObject.SetActive(false);
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
                foreach (var obj in m_ObjectPool)
                {
                    if (obj != null && !obj.activeInHierarchy)
                    {
                        obj.SetActive(true);
                        return obj;
                    }
                }
                var newObj = AddNewInstance();
                return newObj;
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
            var genericNetworkObjectBehaviour = obj.GetComponent<GenericNetworkObjectBehaviour>();
            genericNetworkObjectBehaviour.HasHandler = EnableHandler;
            m_ObjectPool.Add(obj);
            return m_ObjectPool[m_ObjectPool.Count - 1];
        }

        /// <summary>
        /// Starts the
        /// </summary>
        private void StartSpawningBoxes()
        {
            if (NetworkManager.IsHost && SpawnSlider != null)
            {
                SpawnSlider.gameObject.SetActive(true);
            }

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
            if (SpawnSlider != null)
            {
                SpawnsPerSecond = (int)SpawnSlider.value;
                SpawnSliderValueText.text = SpawnsPerSecond.ToString();

                // Handle case where the initial value is set to zero and so coroutine needs to be started
                if (SpawnsPerSecond > 0 && !m_IsSpawningObjects)
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
                                    no.Spawn(true);
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
    public class MyCustomPrefabSpawnHandler : INetworkPrefabInstanceHandler
    {
        private NetworkPrefabPool m_PrefabPool;
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var obj = m_PrefabPool.GetObject();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj.GetComponent<NetworkObject>();
        }
        public void Destroy(NetworkObject networkObject)
        {
            networkObject.transform.position = new Vector3(0, 0, 0);
            networkObject.gameObject.SetActive(false);
        }

        public MyCustomPrefabSpawnHandler(NetworkPrefabPool objectPool)
        {
            m_PrefabPool = objectPool;
        }
    }
}
