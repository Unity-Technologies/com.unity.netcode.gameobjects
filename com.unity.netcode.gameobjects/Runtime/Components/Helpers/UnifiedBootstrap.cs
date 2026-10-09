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
        private static readonly List<NetcodeWorld> s_CreatedWorlds = new List<NetcodeWorld>();

        private static void RegisterCreatedWorlds()
        {
            for (int i = s_CreatedWorlds.Count - 1; i >= 0; i--)
            {
                var world = s_CreatedWorlds[i];
                if (!world.IsCreated)
                {
                    s_CreatedWorlds.RemoveAt(i);
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

                if (networkManager.LogLevel <= LogLevel.Developer)
                {
                    NetworkLog.LogInfo($"[{nameof(UnifiedBootstrap)}] Created world: {LastCreatedWorld.Name} / {LastCreatedWorld.SequenceNumber}");
                }

                networkManager.NetcodeWorld = (NetcodeWorld)LastCreatedWorld;
                s_CreatedWorlds.Add(networkManager.NetcodeWorld);
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
