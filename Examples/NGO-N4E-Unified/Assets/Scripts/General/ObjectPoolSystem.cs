using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using Unity.Netcode.Editor;
/// <summary>
/// The custom editor for the <see cref="AsteroidObject"/> component.
/// </summary>
[CustomEditor(typeof(ObjectPoolSystem), true)]
public class ObjectPoolSystemEditor : NetcodeEditorBase<ObjectPoolSystem>
{
    private SerializedProperty m_NetworkPrefab;
    private SerializedProperty m_ObjectPoolSize;
    private SerializedProperty m_PoolInSystemScene;
    private SerializedProperty m_UsePoolForSpawn;
    private SerializedProperty m_DontDestroyOnSceneUnload;
    private SerializedProperty m_ExtendedProperties;
    private SerializedProperty m_UseUnreliableDeltas;
    private SerializedProperty m_DebugHandlerDestroy;
    private SerializedProperty m_EnableTransformOverrides;
    private SerializedProperty m_HalfFloat;
    private SerializedProperty m_QuaternionSynchronization;
    private SerializedProperty m_QuaternionCompression;
    private SerializedProperty m_Interpolate;

    public override void OnEnable()
    {
        m_NetworkPrefab = serializedObject.FindProperty(nameof(ObjectPoolSystem.NetworkPrefab));
        m_ObjectPoolSize = serializedObject.FindProperty(nameof(ObjectPoolSystem.ObjectPoolSize));
        m_PoolInSystemScene = serializedObject.FindProperty(nameof(ObjectPoolSystem.PoolInSystemScene));
        m_UsePoolForSpawn = serializedObject.FindProperty(nameof(ObjectPoolSystem.UsePoolForSpawn));
        m_DontDestroyOnSceneUnload = serializedObject.FindProperty(nameof(ObjectPoolSystem.DontDestroyOnSceneUnload));
        m_ExtendedProperties = serializedObject.FindProperty(nameof(ObjectPoolSystem.ExtendedProperties));
        m_UseUnreliableDeltas = serializedObject.FindProperty(nameof(ObjectPoolSystem.UseUnreliableDeltas));
        m_DebugHandlerDestroy = serializedObject.FindProperty(nameof(ObjectPoolSystem.DebugHandlerDestroy));
        m_EnableTransformOverrides = serializedObject.FindProperty(nameof(ObjectPoolSystem.EnableTransformOverrides));
        m_HalfFloat = serializedObject.FindProperty(nameof(ObjectPoolSystem.HalfFloat));
        m_QuaternionSynchronization = serializedObject.FindProperty(nameof(ObjectPoolSystem.QuaternionSynchronization));
        m_QuaternionCompression = serializedObject.FindProperty(nameof(ObjectPoolSystem.QuaternionCompression));
        m_Interpolate = serializedObject.FindProperty(nameof(ObjectPoolSystem.Interpolate));
        base.OnEnable();
    }

    private void DisplayObjectPoolSystemProperties()
    {
        var objectPoolSystem = target as ObjectPoolSystem;
        EditorGUILayout.PropertyField(m_NetworkPrefab);
        EditorGUILayout.PropertyField(m_ObjectPoolSize);
        EditorGUILayout.PropertyField(m_PoolInSystemScene);
        EditorGUILayout.PropertyField(m_UsePoolForSpawn);
        EditorGUILayout.PropertyField(m_DontDestroyOnSceneUnload);
        EditorGUILayout.PropertyField(m_ExtendedProperties);
        if (objectPoolSystem.ExtendedProperties)
        {
            EditorGUILayout.PropertyField(m_UseUnreliableDeltas);
            EditorGUILayout.PropertyField(m_DebugHandlerDestroy);
            // Once the prefab is assigned
            if (objectPoolSystem.NetworkPrefab)
            {
                // Check to see if it has a NetworkTransform (or derived from) component
                var networkTransform = objectPoolSystem.NetworkPrefab.GetComponent<NetworkTransform>();
                if (networkTransform)
                {
                    // If so, then provide additional options
                    EditorGUILayout.PropertyField(m_EnableTransformOverrides);
                    if (objectPoolSystem.EnableTransformOverrides)
                    {
                        EditorGUILayout.PropertyField(m_HalfFloat);
                        EditorGUILayout.PropertyField(m_QuaternionSynchronization);
                        EditorGUILayout.PropertyField(m_QuaternionCompression);
                        EditorGUILayout.PropertyField(m_Interpolate);
                    }
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        var objectPoolSystem = target as ObjectPoolSystem;
        void SetExpanded(bool expanded) { objectPoolSystem.ObjectPoolSystemPropertiesVisible = expanded; };
        DrawFoldOutGroup<ObjectPoolSystem>(objectPoolSystem.GetType(), DisplayObjectPoolSystemProperties, objectPoolSystem.ObjectPoolSystemPropertiesVisible, SetExpanded);
        base.OnInspectorGUI();
    }
}
#endif

public interface IPoolSystemTracker
{
    void TrackPoolSystemLoading(ObjectPoolSystem poolSystem, float progress, bool isLoading = true);
}

/// <summary>
/// Generic NetworkObject pool system used throughout the demo.
/// </summary>
public class ObjectPoolSystem : MonoBehaviour, INetworkPrefabInstanceHandler
{
#if UNITY_EDITOR
    public bool ObjectPoolSystemPropertiesVisible = false;
#endif

    public static Dictionary<GameObject, ObjectPoolSystem> ExistingPoolSystems = new Dictionary<GameObject, ObjectPoolSystem>();

    private static List<IPoolSystemTracker> s_PoolSystemTrackers = new List<IPoolSystemTracker>();

    public static void PoolSystemTrackerRegistration(IPoolSystemTracker tracker, bool register = true)
    {
        if (register)
        {
            if (!s_PoolSystemTrackers.Contains(tracker))
            {
                s_PoolSystemTrackers.Add(tracker);
            }
        }
        else
        {
            s_PoolSystemTrackers.Remove(tracker);
        }
    }

    private void UpdatePoolSystemTrackers(ObjectPoolSystem poolSystem, float progress, bool isLoading = true)
    {
        foreach (var tracker in s_PoolSystemTrackers)
        {
            tracker.TrackPoolSystemLoading(poolSystem, progress, isLoading);
        }
    }

    public static ObjectPoolSystem GetPoolSystem(GameObject gameObject)
    {
        if (ExistingPoolSystems.ContainsKey(gameObject))
        {
            return ExistingPoolSystems[gameObject];
        }
        return null;
    }

    [Tooltip("The network prefab to pool.")]
    public GameObject NetworkPrefab;

    [Tooltip("How many instances of the network prefab you want available")]
    public int ObjectPoolSize;

    [Tooltip("For organization purposes: when true, non-spawned instances will be migrated to the object pool's scene. (default is true)")]
    public bool PoolInSystemScene = true;

    [Tooltip("When enabled, the pool will be used to spawn/recylce NetworkObjects")]
    public bool UsePoolForSpawn = true;

    [Tooltip("Enable this to persist the pool objects between sessions (after first load, the pool is pre-loaded).")]
    public bool DontDestroyOnSceneUnload = false;

    [Tooltip("When true, an additional set of properties will be available that you can globally set on all pool object instances.")]
    public bool ExtendedProperties = false;

    [Tooltip("When true, the spawned objects will be configured to use unreliable deltas. Use this option to prevent stutter if packets are dropped due to poor network conditions.")]
    public bool UseUnreliableDeltas = true;

    [Tooltip("When true, debug info will be logged about when objects are despawned and returned to the pool.")]
    public bool DebugHandlerDestroy = false;

    [Tooltip("When enabled, this will expose more transform settings are applied to all spawned NetworkObjects.")]
    public bool EnableTransformOverrides;
    [Tooltip("Enables half float precision.")]
    public bool HalfFloat;
    [Tooltip("Enables quaternion synchronization.")]
    public bool QuaternionSynchronization;
    [Tooltip("Enables quaternion compression.")]
    public bool QuaternionCompression;
    [Tooltip("Enables interpolation.")]
    public bool Interpolate;

    [Tooltip("When enabled, this pool will rebuild itself each time it is initialized upon loading a scene.")]
    public bool ForceRebuildPool;

    private Stack<NetworkObject> m_AvailableObjects = new Stack<NetworkObject>();

    /// <summary>
    /// When a pooled object's state changes (active to not-active in the scene hierarchy), this method is invoked.
    /// </summary>
    private void HandleInstanceStateChange(GameObject instance, bool isSpawning = false)
    {
        if (PoolInSystemScene)
        {
            if (!isSpawning)
            {
                if (instance.transform.parent != null)
                {
                    instance.transform.SetParent(null);
                }
                if (gameObject.scene.IsValid())
                {
                    SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
                }
            }
            else
            {
                SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
            }
        }
        instance.SetActive(isSpawning);
    }

    private void Start()
    {
        ExtendedNetworkManager.OnExiting += OnExiting;
        if (ForceRebuildPool && ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            CleanOutPool();
        }
        Initialize();
    }

    private void Initialize()
    {
        if (!ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            NetworkManager.Singleton.PrefabHandler.AddHandler(NetworkPrefab, this);
            ExistingPoolSystems.Add(NetworkPrefab, this);
            if (DontDestroyOnSceneUnload)
            {
                DontDestroyOnLoad(gameObject);
            }
            StartCoroutine(CreatePrefabPool());
        }
        else
        {
            // This is registers the prefab handler with NetworkManager
            NetworkManager.Singleton.PrefabHandler.AddHandler(NetworkPrefab, ExistingPoolSystems[NetworkPrefab]);

            // This provides the mechanism that tracks the status of the object pool instance when first instantiating all objects.
            ExtendedNetworkManager.Instance.TrackPoolSystemLoading(ExistingPoolSystems[NetworkPrefab], 1.0f);
            if (DontDestroyOnSceneUnload)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        if ((ForceRebuildPool || !DontDestroyOnSceneUnload) && ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            CleanOutPool();
        }
    }

    private void OnClientStarted()
    {
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    private void OnExiting()
    {
        ExtendedNetworkManager.OnExiting -= OnExiting;
        if (ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            if (ExtendedNetworkManager.Instance.IsListening)
            {
                ExtendedNetworkManager.Instance.PrefabHandler.RemoveHandler(NetworkPrefab);
                CleanOutPool();
            }
        }
    }

    private void OnClientStopped(bool obj)
    {
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        if (ForceRebuildPool && ExistingPoolSystems.ContainsKey(NetworkPrefab))
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(NetworkPrefab);
            CleanOutPool();
            Initialize();
        }
    }


    /// <summary>
    /// Coroutine that instantiates all of the objects over time
    /// </summary>
    private IEnumerator CreatePrefabPool()
    {
        var splitCount = (int)ObjectPoolSize * 0.1f;

        while (m_AvailableObjects.Count < ObjectPoolSize)
        {
            for (int i = 0; i < splitCount; i++)
            {
                var instance = Instantiate(NetworkPrefab);
                instance.name = instance.name.Replace("(Clone)", "");
                instance.name += $"_{m_AvailableObjects.Count}";
                HandleInstanceStateChange(instance);
                var networkObject = instance.GetComponent<NetworkObject>();
                networkObject.SetSceneObjectStatus();
                if (ExtendedProperties)
                {
                    var networkTransforms = instance.GetComponentsInChildren<NetworkTransform>();
                    foreach (var networkTransform in networkTransforms)
                    {
                        networkTransform.UseUnreliableDeltas = UseUnreliableDeltas;
                        if (networkTransform != null && EnableTransformOverrides)
                        {
                            networkTransform.UseHalfFloatPrecision = HalfFloat;
                            networkTransform.UseQuaternionSynchronization = QuaternionSynchronization;
                            networkTransform.UseQuaternionCompression = QuaternionCompression;
                            networkTransform.Interpolate = Interpolate;
                        }
                    }
                }

                m_AvailableObjects.Push(networkObject);
                // When not being used, parent under the pool system to make heirarchy browsing easier
                // Turn off AutoObjectParentSync to avoid any errors with parenting
                networkObject.AutoObjectParentSync = false;
                instance.transform.parent = transform;
                if (m_AvailableObjects.Count >= ObjectPoolSize)
                {
                    break;
                }
                UpdatePoolSystemTrackers(this, m_AvailableObjects.Count / (float)ObjectPoolSize);
            }
            yield return null;
        }
        UpdatePoolSystemTrackers(this, m_AvailableObjects.Count / (float)ObjectPoolSize);
    }

    private void CleanOutPool()
    {
        foreach (var poolObject in m_AvailableObjects)
        {
            if (poolObject != null && poolObject.gameObject != null)
            {
                Destroy(poolObject.gameObject);
            }
        }

        m_AvailableObjects.Clear();
        ExistingPoolSystems.Remove(NetworkPrefab);
    }

    /// <summary>
    /// The owner will use this method to pull already existing objects from the pool
    /// </summary>

    public NetworkObject GetInstance(bool isSpawningLocally = false)
    {
        var returnValue = (NetworkObject)null;

        if (m_AvailableObjects.TryPop(out NetworkObject instance))
        {
            // When being used, remove the parent and turn AutoObjectParentSync back on again
            instance.transform.parent = null;
            instance.AutoObjectParentSync = true;
            HandleInstanceStateChange(instance.gameObject, true);
            instance.DeferredDespawnTick = 0;
            returnValue = instance;
        }
        else
        {
            if (NetworkManager.Singleton.LogLevel >= LogLevel.Developer)
            {
                NetworkLog.LogWarningServer($"[Object Pool ({name}) Exhausted] Instantiating new instances during network session!");
            }
            returnValue = Instantiate(NetworkPrefab).GetComponent<NetworkObject>();
            returnValue.gameObject.name += "_NP";
        }

        if (isSpawningLocally)
        {
            var networkTransform = returnValue.GetComponent<NetworkTransform>();
            if (networkTransform != null && EnableTransformOverrides)
            {
                networkTransform.UseHalfFloatPrecision = HalfFloat;
                networkTransform.UseQuaternionSynchronization = QuaternionSynchronization;
                networkTransform.UseQuaternionCompression = QuaternionCompression;
                networkTransform.Interpolate = Interpolate;
            }
        }
        return returnValue;
    }

    /// <summary>
    /// Non-owners will have this method called when the object is spawned on their side.
    /// </summary>
    /// <returns>the object instance to spawn locally</returns>
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        var instance = GetInstance(!NetworkManager.Singleton.DistributedAuthorityMode && NetworkManager.Singleton.LocalClientId == ownerClientId);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        return instance;
    }



    /// <summary>
    /// Invoked when a spawned object from the pool is despawned and destroyed
    /// </summary>
    public void Destroy(NetworkObject networkObject)
    {
        if (networkObject.gameObject.name.Contains("_NP"))
        {
            Destroy(networkObject.gameObject);
        }
        else
        {
            if (!DebugHandlerDestroy)
            {
                HandleInstanceStateChange(networkObject.gameObject);
                m_AvailableObjects.Push(networkObject);
            }
            else
            {
                if (networkObject.IsSpawned)
                {
                    Debug.LogError($"[{networkObject.name}] Is still spawned but is being put back into pool!");
                }
                if (!m_AvailableObjects.Contains(networkObject))
                {
                    HandleInstanceStateChange(networkObject.gameObject);
                    m_AvailableObjects.Push(networkObject);
                }
                else
                {
                    Debug.LogError($"[ObjectPoolSystem] PrefabHandler invoked twice for {networkObject.name}!");
                }
            }
            networkObject.transform.position = Vector3.zero;
            networkObject.transform.rotation = Quaternion.identity;
            // When not being used, parent under the pool system to make heirarchy browsing easier
            // Turn off AutoObjectParentSync to avoid any errors with parenting
            networkObject.AutoObjectParentSync = false;
            networkObject.transform.parent = transform;
        }
    }
}