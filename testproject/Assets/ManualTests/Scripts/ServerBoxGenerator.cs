using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Spawning;
using MLAPI.SceneManagement;

public class ServerBoxGenerator : NetworkBehaviour
{

    public static ServerBoxGenerator Singleton;

    public float InitialSpawnDelay;
    public int spawnsPerSecond;    
    public int BoxPoolSize;
    public float BoxSpeed = 10.0f;    
    public bool EnableNetworkPrefabInstanceHandler;
    public GameObject ServerObjectToPool;
    public GameObject ClientObjectToPool;
    public SwitchSceneHandler SwitchScene;

    public Slider spawnSlider;
    public Text spawnSliderValueText;

    private NetworkVariable<int> spawns_nv = new NetworkVariable<int>(0);
    private int m_NextBulletId = 0;

    private float EntitiesPerFrame;
    private GameObject ObjectToSpawn;

    private List<GameObject> objects;
    private bool m_NetworkHasStarted;
    
    private MyCustomPrefabSpawnHandler myCustomPrefabSpawnHandler;


    void Awake()
    {
        if (Singleton != null)
        {
            GameObject.Destroy(Singleton);
            Singleton = null;
        }
        Singleton = this;

        Screen.SetResolution(1024, 768, FullScreenMode.Windowed);
    }

    private void OnEnable()
    {
        if (spawnSlider != null)
        {
            spawnSlider.gameObject.SetActive(false);
        }

        if (myCustomPrefabSpawnHandler == null && EnableNetworkPrefabInstanceHandler && NetworkManager && NetworkManager.PrefabHandler != null)
        {
            myCustomPrefabSpawnHandler = new MyCustomPrefabSpawnHandler(this);
            NetworkManager.PrefabHandler.AddHandler(ServerObjectToPool, myCustomPrefabSpawnHandler);
        }
    }

    private void OnDestroy()
    {
        if (SwitchScene)
        {
            SwitchScene.OnSceneSwitchBegin -= OnSceneSwitchBegin;
        }
        if (NetworkManager && EnableNetworkPrefabInstanceHandler && myCustomPrefabSpawnHandler != null)
        {
            var no = ServerObjectToPool.GetComponent<NetworkObject>();
            NetworkManager.PrefabHandler.RemoveHandler(no);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (spawnSliderValueText != null)
        {
            spawnSliderValueText.text = spawnsPerSecond.ToString();
        }

        if (SwitchScene)
        {
            SwitchScene.OnSceneSwitchBegin += OnSceneSwitchBegin;
        }

        if (myCustomPrefabSpawnHandler == null && EnableNetworkPrefabInstanceHandler && NetworkManager && NetworkManager.PrefabHandler != null)
        {
            myCustomPrefabSpawnHandler = new MyCustomPrefabSpawnHandler(this);
            NetworkManager.PrefabHandler.AddHandler(ServerObjectToPool, myCustomPrefabSpawnHandler);
        }
    }

    private void OnSceneSwitchBegin()
    {
        if (IsServer)
        {
            StopCoroutine(SpawnBoxes());

            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    var networkObject = obj.GetComponent<NetworkObject>();
                    if (networkObject.IsSpawned)
                    {
                        networkObject.Despawn();
                    }
                    GameObject.Destroy(obj);
                }
                objects.Clear();
            }
        }
    }

    private float m_DelaySpawning;

    public override void NetworkStart()
    {
        Initialize();
        if (IsServer)
        {
            if (isActiveAndEnabled)
            {
                m_DelaySpawning = Time.realtimeSinceStartup + InitialSpawnDelay;
                StartSpawningBoxes();
            }
        }
        base.NetworkStart();
    }

    private void OnDisable()
    {
        IsSpawningBoxes = false;
    }

    void StartSpawningBoxes()
    {

        if (NetworkManager.Singleton.IsHost && spawnSlider != null)
        {
            spawnSlider.gameObject.SetActive(true);
        }

        if (spawnsPerSecond > 0)
        {
            StartCoroutine(SpawnBoxes());
        }

    }

    public void Initialize()
    {
        
        ObjectToSpawn = ServerObjectToPool;
        if (!IsServer && EnableNetworkPrefabInstanceHandler)
        {
            ObjectToSpawn = ClientObjectToPool;
        }

        objects = new List<GameObject>(BoxPoolSize);

        for (int i = 0; i < BoxPoolSize; i++)
        {
            AddNewInstance();
        }
    }


    public GameObject GetObject()
    {
        if (objects != null)
        {
            foreach (var obj in objects)
            {
                if (obj.activeInHierarchy) continue;

                obj.SetActive(true);
                return obj;
            }
#if UNITY_EDITOR
            Debug.Log("Object pool exhausted. Growing.", this);
#endif
            var newObj = AddNewInstance();
            newObj.SetActive(true);
            return newObj;
        }
        return null;
    }


    private GameObject AddNewInstance()
    {
        var obj = Instantiate(ObjectToSpawn);
        var no = obj.GetComponent<NetworkObject>();
        obj.SetActive(false);
        objects.Add(obj);
        return obj;
    }



    public void UpdateSpawnsPerSecond()
    {
        if (spawnSlider != null)
        {
            spawnsPerSecond = (int)spawnSlider.value;
            spawnSliderValueText.text = spawnsPerSecond.ToString();
        }
    }

    bool IsSpawningBoxes;
    IEnumerator SpawnBoxes()
    {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
        {
            yield return null;
        }

        if(m_DelaySpawning > Time.realtimeSinceStartup)
        {
            yield return new WaitForSeconds(m_DelaySpawning - Time.realtimeSinceStartup); 
        }

        IsSpawningBoxes = true;
        while (IsSpawningBoxes)
        {
            float EntitySpawnUpdateRate = 1.0f;
            if (spawnsPerSecond > 0)
            {
                EntitySpawnUpdateRate = 1.0f / Mathf.Min(spawnsPerSecond, 60.0f);
                //While not 100% accurate, this basically allows for higher entities per second generation
                EntitiesPerFrame = (float)spawnsPerSecond * EntitySpawnUpdateRate;
                int EntitityCountPerFrame = Mathf.RoundToInt(EntitiesPerFrame);
                //Spawn (n) entities then wait for 1/60th of a second and repeat
                for (int i = 0; i < EntitityCountPerFrame; i++)
                {
                    GameObject go = GetObject();
                    if (go != null)
                    {
                        go.transform.position = transform.position;

                        float ang = Random.Range(0.0f, 2 * Mathf.PI);
                        //go.GetComponent<Rigidbody>().velocity = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)).normalized * BoxSpeed;
                        go.GetComponent<Bullet>().SetDirectionAndVelocity(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)), BoxSpeed);
                        go.GetComponent<Bullet>().SetId(m_NextBulletId);
                        m_NextBulletId++;

                        var no = go.GetComponent<NetworkObject>();
                        if (!no.IsSpawned)
                        {
                            spawns_nv.Value++;
                            no.Spawn(null, true);
                        }
                    }
                }
            }
            yield return new WaitForSeconds(EntitySpawnUpdateRate);
        }

    }
}

public class MyCustomPrefabSpawnHandler : INetworkPrefabInstanceHandler
{
    private ServerBoxGenerator m_ObjectPool;
    public NetworkObject HandleNetworkPrefabSpawn(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        var obj = m_ObjectPool.GetObject();
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj.GetComponent<NetworkObject>();
    }
    public void HandleNetworkPrefabDestroy(NetworkObject networkObject)
    {
        networkObject.transform.position = Vector3.zero;
        networkObject.gameObject.SetActive(false);
    }

    public MyCustomPrefabSpawnHandler(ServerBoxGenerator objectPool)
    {
        m_ObjectPool = objectPool;
    }

    public void Dispose()
    {

    }
}
