using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Spawning;

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

            //This registers early under the condition of a scene transition
            RegisterCustomPrefabHandler();
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

                SwitchScene.OnSceneSwitchCompleted -= SwitchScene_OnSceneSwitchCompleted;
            }
        }

        private void SwitchScene_OnSceneSwitchCompleted()
        {
            InitializeObjectPool();
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
                SwitchScene.OnSceneSwitchCompleted += SwitchScene_OnSceneSwitchCompleted;
            }

            //Call this again in case we didn't have access to the NetworkManager already (i.e. first scene loaded)
            RegisterCustomPrefabHandler();

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
            // Start by defining the server only network prefab for pooling
            m_ObjectToSpawn = ServerObjectToPool;

            // If we are a host, then we have to get the NetworkPrefab Override (if one exists)
            if (IsHost && !EnableHandler)
            {
                m_ObjectToSpawn = NetworkManager.GetNetworkPrefabOverride(ServerObjectToPool);
            }
            // If we are a client and we are using the custom prefab override handler, then we need to use that for our pool
            // This also checks to see if the ClientObjectToPool is set, if not then we are just using the custom prefab override handler
            // to assure the client-side uses the NetworkObject pool as opposed to always spawning and destroying NetworkObjects.
            else if (IsClient && EnableHandler && ClientObjectToPool != null)
            {
                m_ObjectToSpawn = ClientObjectToPool;
            }

            // If we are enabling the handler, then we can control which NetworkObject will be used for spawning.
            // If we are the server but do not have a handler, then we use a less efficient server-side only pool (clients will instantiate and destroy on their side)
            if (EnableHandler || IsServer)
            {
                // In order to account for any NetworkPrefab override defined within the NetworkManager, we do one last check to assure we are creating a pool
                // of the right NetworkPrefab objects, otherwise GetNetworkPrefabOverride will return back the same m_ObjectToSpawn
                // NOTE: We filter out the case where we are a server, as the server will send the original NetworkPrefab GlobalObjectIdHash.
                // If we enable this for dedicated server, then the server would spawn the override prefab which will cause the client to create a pool that is
                // never used and the client(s) will spawn and destroy GameObjects outside of the pool.
                if (EnableHandler && IsClient)
                {
                    m_ObjectToSpawn = NetworkManager.GetNetworkPrefabOverride(m_ObjectToSpawn);
                }

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
                foreach (var obj in m_ObjectPool)
                {
                    if (!obj.activeInHierarchy)
                    {
                        obj.SetActive(true);
                        return obj;
                    }
                }
                var newObj = AddNewInstance();
                newObj.SetActive(true);
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
            var no = obj.GetComponent<NetworkObject>();
            obj.SetActive(false);
            m_ObjectPool.Add(obj);
            return obj;
        }

        /// <summary>
        /// Starts spawning
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
    public class MyCustomPrefabSpawnHandler : INetworkPrefabInstanceHandler
    {
        private NetworkPrefabPool m_PrefabPool;
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

        public MyCustomPrefabSpawnHandler(NetworkPrefabPool objectPool)
        {
            m_PrefabPool = objectPool;
        }
    }
}
