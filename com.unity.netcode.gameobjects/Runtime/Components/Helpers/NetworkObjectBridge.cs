#if UNIFIED_NETCODE
using System;
using Unity.Entities;
using Unity.NetCode;

namespace Unity.Netcode
{

    /// <summary>
    /// TODO-UNIFIED: Would need to be reviewed for alternate ways of handling this.
    /// </summary>
    /// <remarks>
    /// If used, we most likely would make this internal
    /// </remarks>
    public partial class NetworkObjectBridge : GhostBehaviour
    {
        public Action<ulong> NetworkObjectIdChanged;
        
        internal GhostField<ulong> NetworkObjectId = new GhostField<ulong>();

        public void SetNetworkObjectId(ulong value)
        {
            NetworkObjectId.Value = value;
        }
        public override void Awake()
        {
            if (UnifiedBootStrap.Instance != null)
            {
                Initialize();
            }
            else
            {
                UnifiedBootStrap.OnInitialized += Initialize;
            }
        }

        private void Initialize()
        {
            UnifiedBootStrap.OnInitialized -= Initialize;
            if (gameObject != null)
            {
                base.Awake();
                NetworkObjectId.ValueChanged += OnNetworkObjectIdChanged;
            }
        }

        private void OnNetworkObjectIdChanged(ulong value)
        {
            NetworkObjectIdChanged?.Invoke(value);
        }

        internal void OnDespawn(bool shouldDestroy)
        {
            if (shouldDestroy)
            {
                UnifiedBootStrap.OnInitialized -= Initialize;
            }
        }

        public override void OnDestroy()
        {
            UnifiedBootStrap.OnInitialized -= Initialize;
            base.OnDestroy();
        }
    }

    /// <summary>
    /// TODO-UNIFIED: Would need to be reviewed for alternate ways of handling this.
    /// Creates the hosted world and provides a means to configuring
    /// the 2nd port for unified netcode connection.
    /// </summary>
    internal class UnifiedBootStrap : ClientServerBootstrap
    {
        public static UnifiedBootStrap Instance { get; private set; }
        public static Action OnInitialized;
        public static ushort Port = 7979;

        public static World World { get; private set; }

        public override bool Initialize(string defaultWorldName)
        {
            var networkManager = NetworkManager.Singleton;
            Instance = this;
            AutoConnectPort = Port;
            if (base.Initialize(defaultWorldName))
            {
                UnityEngine.Debug.LogError($"[{nameof(UnifiedBootStrap)}] Auto-bootstrap is enabled!!! This will break the POC!");
                return true;
            }

            World = networkManager.IsServer ? CreateSingleWorldHost("ClientAndServerWorld") : CreateClientWorld("ClientWorld");

            if (World == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(UnifiedBootStrap)}] World is null!");
                return false;
            }

            if (!World.IsCreated)
            {
                UnityEngine.Debug.LogError($"[{nameof(UnifiedBootStrap)}] World was not created!");
                return false;
            }

            if (networkManager.LogLevel <= LogLevel.Developer)
            {
                NetworkLog.LogInfo($"[{nameof(UnifiedBootStrap)}] Created world: {World.Name}");
            }

            if (networkManager.NetworkConfig.Prefabs.HasPendingGhostPrefabs)
            {
                if (networkManager.LogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"[{nameof(UnifiedBootStrap)}] Registering hybrid prefabs...");
                }
                networkManager.NetworkConfig.Prefabs.RegisterGhostPrefabs(networkManager);
            }

            OnInitialized?.Invoke();

            return true;
        }

        ~UnifiedBootStrap()
        {
            Instance = null;
        }
    }
}
#endif
