#if UNIFIED_NETCODE
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// TODO-UNIFIED: Needs furthery review and proper error messaging using the new logging context.
    /// Handles the bootstrap process for both client and server in unified mode. This is used to create the world and set it on the
    /// NetworkManager during initialization.
    /// </summary>
    /// <remarks>
    /// Entities picks this as the startup bootstrap, which only creates a local world. Do not add
    /// [DisableBootstrapOverrides]: Netcode for Entities' own bootstrap would then run and, unless automatic
    /// bootstrapping is disabled, create a host world that holds the port a hybrid session listens on.
    /// </remarks>
    internal class UnifiedBootstrap : ClientServerBootstrap
    {
        public static UnifiedBootstrap Instance { get; private set; }
        public static Action OnInitialized;
        public static ushort Port = 7979;
        public static NetworkManager CurrentNetworkManagerForInitialization;

        public static World LastCreatedWorld { get; private set; }

        private static int s_WorldCounter = 0;

        // Every ClientServerBootstrap constructor clears N4E's ServerWorlds and ClientWorlds, and each NetworkManager
        // creates its own bootstrap, so the worlds created for other NetworkManagers are registered again.
        private static readonly List<NetcodeWorld> k_CreatedWorlds = new List<NetcodeWorld>();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnLoad()
        {
            Instance = null;
            OnInitialized = null;
            Port = 7979;
            CurrentNetworkManagerForInitialization = null;
            LastCreatedWorld = null;
            s_WorldCounter = 0;
            k_CreatedWorlds.Clear();
        }
#endif

        private static void RegisterCreatedWorlds()
        {
            for (int i = k_CreatedWorlds.Count - 1; i >= 0; i--)
            {
                var world = k_CreatedWorlds[i];
                if (!world.IsCreated)
                {
                    k_CreatedWorlds.RemoveAt(i);
                    continue;
                }
                // A single world host is registered as both a server and a client world, the same as N4E does.
                if (world.IsServer() && !ServerWorlds.Contains(world))
                {
                    ServerWorlds.Add(world);
                }
                if (world.IsClient() && !ClientWorlds.Contains(world))
                {
                    ClientWorlds.Add(world);
                }
            }
        }

        public override bool Initialize(string defaultWorldName)
        {
            var networkManager = CurrentNetworkManagerForInitialization;
            if (networkManager == NetworkManager.Singleton)
            {
                Instance = this;
            }

            AutoConnectPort = Port;

            // NetworkManager owns world creation here, so base.Initialize is deliberately not called: it would
            // let Netcode for Entities create worlds this bootstrap then has to reject.

            if (networkManager != null)
            {
                RegisterCreatedWorlds();
                Debug.Log($"Starting a world for {(networkManager.IsServer ? "Host" : "Client")}");
                s_WorldCounter++;
                var world = CreateWorld(networkManager);
                LastCreatedWorld = world;

                if (world == null)
                {
                    s_WorldCounter--;
                    Debug.LogError($"[{nameof(UnifiedBootstrap)}] World is null!");
                    return false;
                }

                if (!world.IsCreated)
                {
                    s_WorldCounter--;
                    Debug.LogError($"[{nameof(UnifiedBootstrap)}] World was not created!");
                    return false;
                }

                if (networkManager.LogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"[{nameof(UnifiedBootstrap)}] Created world: {world.Name} / {world.SequenceNumber}");
                }

                networkManager.NetcodeWorld = (NetcodeWorld)world;
                k_CreatedWorlds.Add(networkManager.NetcodeWorld);
            }
            else
            {
                LastCreatedWorld = CreateLocalWorld("LocalWorld");
            }

            // Always wrap events that can invoke user script in a try-catch to assure any
            // proceeding script is still executed.
            try
            {
                OnInitialized?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return true;
        }

        /// <summary>
        /// Creates the world used by a <see cref="NetworkManager"/>'s hybrid session.
        /// </summary>
        /// <remarks>
        /// A server or host uses a single world host. A dedicated server build uses a server-only world instead, because
        /// Netcode for Entities cannot create a client world there. A build that only targets the client or the server
        /// cannot start a host.
        /// </remarks>
        private static World CreateWorld(NetworkManager networkManager)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            if (networkManager.IsClient)
            {
                Debug.LogError($"[{nameof(UnifiedBootstrap)}] A dedicated server build cannot start a {(networkManager.IsServer ? "host" : "client")} in hybrid mode.");
                return null;
            }
            return CreateServerWorld($"ServerWorld-{s_WorldCounter}");
#elif UNITY_CLIENT && !UNITY_EDITOR
            if (networkManager.IsServer)
            {
                Debug.LogError($"[{nameof(UnifiedBootstrap)}] A client only build cannot start a {(networkManager.IsClient ? "host" : "server")} in hybrid mode. Set the Netcode for Entities client build target to ClientAndServer.");
                return null;
            }
            return CreateClientWorld($"ClientWorld-{s_WorldCounter}");
#else
            return networkManager.IsServer ? CreateSingleWorldHost($"HostSingleWorld-{s_WorldCounter}") : CreateClientWorld($"ClientWorld-{s_WorldCounter}");
#endif
        }
    }
}
#endif
