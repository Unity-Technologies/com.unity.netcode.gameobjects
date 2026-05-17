#if UNIFIED_NETCODE
using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// TODO-UNIFIED: Would need to be reviewed for alternate ways of handling this.
    /// Creates the hosted world and provides a means to configuring
    /// the 2nd port for unified netcode connection.
    /// </summary>
    internal class UnifiedBootstrap : ClientServerBootstrap
    {
        public static UnifiedBootstrap Instance { get; private set; }
        public static Action OnInitialized;
        public static ushort Port = 7979;
        public static NetworkManager CurrentNetworkManagerForInitialization;

        public static World LastCreatedWorld { get; private set; }

        private static int s_WorldCounter = 0;

        public override bool Initialize(string defaultWorldName)
        {
            var networkManager = CurrentNetworkManagerForInitialization;
            if (networkManager == NetworkManager.Singleton)
            {
                Instance = this;
            }

            AutoConnectPort = Port;
            if (base.Initialize(defaultWorldName))
            {
                Debug.LogError($"[{nameof(UnifiedBootstrap)}] Auto-bootstrap is enabled!!! This will break the POC!");
                return true;
            }

            if (networkManager != null)
            {
                Debug.Log($"Starting a world for {(networkManager.IsServer ? "Host" : "Client")}");
                s_WorldCounter++;
                LastCreatedWorld = networkManager.IsServer ? CreateSingleWorldHost($"HostSingleWorld-{s_WorldCounter}")
                    : CreateClientWorld($"ClientWorld-{s_WorldCounter}");

                if (LastCreatedWorld == null)
                {
                    s_WorldCounter--;
                    Debug.LogError($"[{nameof(UnifiedBootstrap)}] World is null!");
                    return false;
                }

                if (!LastCreatedWorld.IsCreated)
                {
                    s_WorldCounter--;
                    Debug.LogError($"[{nameof(UnifiedBootstrap)}] World was not created!");
                    return false;
                }

                //if (networkManager.LogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"[{nameof(UnifiedBootstrap)}] Created world: {LastCreatedWorld.Name} / {LastCreatedWorld.SequenceNumber}");
                }

                networkManager.NetcodeWorld = (NetcodeWorld)LastCreatedWorld;
#if UNIFIED_NGO_REGISTERS_PREFABS
                if (networkManager.NetworkConfig.Prefabs.HasPendingGhostPrefabs)
                {
                    if (networkManager.LogLevel <= LogLevel.Developer)
                    {
                        NetworkLog.LogInfo($"[{nameof(UnifiedBootstrap)}] Registering hybrid prefabs...");
                    }
                    networkManager.NetworkConfig.Prefabs.RegisterGhostPrefabs(networkManager);
                }
#endif
            }
            else
            {
                LastCreatedWorld = CreateLocalWorld("LocalWorld");
            }

            OnInitialized?.Invoke();

            return true;
        }

        ~UnifiedBootstrap()
        {
            LastCreatedWorld = null;
            Instance = null;
        }
    }
}
#endif
