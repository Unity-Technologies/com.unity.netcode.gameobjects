#if UNIFIED_NETCODE
using System;
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
                Initialize(true);
            }
            else
            {
                UnifiedBootStrap.OnInitialized += Initialize;
            }
        }

        private void Initialize(bool initialized)
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
        public static Action<bool> OnInitialized;
        public static ushort Port = 7979;

        public override bool Initialize(string defaultWorldName)
        {
            var networkManager = NetworkManager.Singleton;
            Instance = this;
            AutoConnectPort = Port;
            if (networkManager.IsServer)
            {
                CreateSingleWorldHost("ClientAndServerWorld");
                if (networkManager.LogLevel <= LogLevel.Developer)
                {
                    UnityEngine.Debug.Log("Creating world: ClientAndServerWorld");
                }
            }
            else
            {
                CreateClientWorld("ClientWorld");
                if (networkManager.LogLevel <= LogLevel.Developer)
                {
                    UnityEngine.Debug.Log("Creating world: ClientWorld");
                }
            }
            var initialized = base.Initialize(defaultWorldName);
            OnInitialized?.Invoke(initialized);
            return initialized;
        }
        
        public static void StopClient()
        {
            ClientWorld.Dispose();
            ClientWorlds.Remove(ClientWorld);
        }
        
        public static void StopServer()
        {
            ServerWorld.Dispose();
            ServerWorlds.Remove(ServerWorld);
        }

        ~UnifiedBootStrap()
        {
            Instance = null;
        }
    }
}
#endif
