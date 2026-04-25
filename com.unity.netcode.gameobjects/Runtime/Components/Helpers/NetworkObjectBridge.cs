#if UNIFIED_NETCODE
using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Unity.Netcode
{

#if UNIFIED_NETCODE
    /// <summary>
    /// TODO-UNIFIED: Needs further peer review and exploring alternate ways of handling this.
    /// </summary>
    /// <remarks>
    /// If used, we most likely would make this internal
    /// </remarks>
    public partial class NetworkObjectBridge : GhostBehaviour
    {

#if UNITY_EDITOR && !UNITY_INCLUDE_TESTS
        [UnityEngine.HideInInspector]
        [UnityEngine.SerializeField]
        private bool m_Sorted = false;
        private void OnValidate()
        {
            // Sort only once when we have first been added.
            if (!m_Sorted)
            {
                while (UnityEditorInternal.ComponentUtility.MoveComponentUp(this))
                {
                    // Keep moving until it can't go higher
                }
                var ghostAdapter = gameObject.GetComponent<GhostAdapter>();
                // Now move the GhostAdapter to the top so it is above NetworkObjectBridge
                while (ghostAdapter != null && UnityEditorInternal.ComponentUtility.MoveComponentUp(ghostAdapter))
                {
                    // Keep moving until it can't go higher
                }

                m_Sorted = true;
            }
        }
#endif


        /// <summary>
        /// This is used to link <see cref="NetworkObject.SerializedObject"/> data to
        /// N4E-spawned hybrid prefab instances.
        /// </summary>
        internal GhostField<ulong> NetworkObjectId = new GhostField<ulong>();
        public void SetNetworkObjectId(ulong networkObjectId)
        {
            NetworkObjectId.PresetValue(networkObjectId);
            NetworkObjectId.Value = networkObjectId;
        }
    }
#endif

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
                Debug.LogError($"[{nameof(UnifiedBootStrap)}] Auto-bootstrap is enabled!!! This will break the POC!");
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
                    Debug.LogError($"[{nameof(UnifiedBootStrap)}] World is null!");
                    return false;
                }

                if (!LastCreatedWorld.IsCreated)
                {
                    s_WorldCounter--;
                    Debug.LogError($"[{nameof(UnifiedBootStrap)}] World was not created!");
                    return false;
                }

                //if (networkManager.LogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"[{nameof(UnifiedBootStrap)}] Created world: {LastCreatedWorld.Name} / {LastCreatedWorld.SequenceNumber}");
                }

                networkManager.NetcodeWorld = (NetcodeWorld)LastCreatedWorld;
                if (networkManager.NetworkConfig.Prefabs.HasPendingGhostPrefabs)
                {
                    if (networkManager.LogLevel <= LogLevel.Developer)
                    {
                        NetworkLog.LogInfo($"[{nameof(UnifiedBootStrap)}] Registering hybrid prefabs...");
                    }

                    networkManager.NetworkConfig.Prefabs.RegisterGhostPrefabs(networkManager);
                }
            }
            else
            {
                LastCreatedWorld = CreateLocalWorld("LocalWorld");
            }

            OnInitialized?.Invoke();

            return true;
        }

        ~UnifiedBootStrap()
        {
            LastCreatedWorld = null;
            Instance = null;
        }
    }
}
#endif
