using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace MLAPI
{
    /// <summary>
    /// Defines the required interface of a network update system being executed by the network update loop.
    /// </summary>
    public interface INetworkUpdateSystem
    {
        void NetworkUpdate(NetworkUpdateStage updateStage);
    }

    /// <summary>
    /// Defines network update stages being executed by the network update loop.
    /// </summary>
    public enum NetworkUpdateStage : byte
    {
        Initialization = 1,
        EarlyUpdate = 2,
        FixedUpdate = 3,
        PreUpdate = 4,
        Update = 0, // Default
        PreLateUpdate = 5,
        PostLateUpdate = 6
    }

    /// <summary>
    /// Represents the network update loop injected into low-level player loop in Unity.
    /// </summary>
    public static class NetworkUpdateLoop
    {
        private static Dictionary<NetworkUpdateStage, HashSet<INetworkUpdateSystem>> s_UpdateSystem_Sets;
        private static Dictionary<NetworkUpdateStage, INetworkUpdateSystem[]> s_UpdateSystem_Arrays;
        private const int k_UpdateSystem_InitialArrayCapacity = 1024;

        static NetworkUpdateLoop()
        {
            s_UpdateSystem_Sets = new Dictionary<NetworkUpdateStage, HashSet<INetworkUpdateSystem>>();
            s_UpdateSystem_Arrays = new Dictionary<NetworkUpdateStage, INetworkUpdateSystem[]>();

            foreach (NetworkUpdateStage updateStage in Enum.GetValues(typeof(NetworkUpdateStage)))
            {
                s_UpdateSystem_Sets.Add(updateStage, new HashSet<INetworkUpdateSystem>());
                s_UpdateSystem_Arrays.Add(updateStage, new INetworkUpdateSystem[k_UpdateSystem_InitialArrayCapacity]);
            }
        }

        /// <summary>
        /// Registers a network update system to be executed in all network update stages.
        /// </summary>
        public static void RegisterAllNetworkUpdates(this INetworkUpdateSystem updateSystem)
        {
            foreach (NetworkUpdateStage updateStage in Enum.GetValues(typeof(NetworkUpdateStage)))
            {
                RegisterNetworkUpdate(updateSystem, updateStage);
            }
        }

        /// <summary>
        /// Registers a network update system to be executed in a specific network update stage.
        /// </summary>
        public static void RegisterNetworkUpdate(this INetworkUpdateSystem updateSystem, NetworkUpdateStage updateStage = NetworkUpdateStage.Update)
        {
            var sysSet = s_UpdateSystem_Sets[updateStage];
            if (!sysSet.Contains(updateSystem))
            {
                sysSet.Add(updateSystem);

                int setLen = sysSet.Count;
                var sysArr = s_UpdateSystem_Arrays[updateStage];
                int arrLen = sysArr.Length;

                if (setLen > arrLen)
                {
                    // double capacity
                    sysArr = s_UpdateSystem_Arrays[updateStage] = new INetworkUpdateSystem[arrLen *= 2];
                }

                sysSet.CopyTo(sysArr);

                if (setLen < arrLen)
                {
                    // null terminator
                    sysArr[setLen] = null;
                }
            }
        }

        /// <summary>
        /// Unregisters a network update system from all network update stages.
        /// </summary>
        public static void UnregisterAllNetworkUpdates(this INetworkUpdateSystem updateSystem)
        {
            foreach (NetworkUpdateStage updateStage in Enum.GetValues(typeof(NetworkUpdateStage)))
            {
                UnregisterNetworkUpdate(updateSystem, updateStage);
            }
        }

        /// <summary>
        /// Unregisters a network update system from a specific network update stage.
        /// </summary>
        public static void UnregisterNetworkUpdate(this INetworkUpdateSystem updateSystem, NetworkUpdateStage updateStage = NetworkUpdateStage.Update)
        {
            var sysSet = s_UpdateSystem_Sets[updateStage];
            if (sysSet.Contains(updateSystem))
            {
                sysSet.Remove(updateSystem);

                int setLen = sysSet.Count;
                var sysArr = s_UpdateSystem_Arrays[updateStage];
                int arrLen = sysArr.Length;

                sysSet.CopyTo(sysArr);

                if (setLen < arrLen)
                {
                    // null terminator
                    sysArr[setLen] = null;
                }
            }
        }

        /// <summary>
        /// The current network update stage being executed.
        /// </summary>
        public static NetworkUpdateStage UpdateStage;

        private static void RunNetworkUpdateStage(NetworkUpdateStage updateStage)
        {
            UpdateStage = updateStage;

            var sysArr = s_UpdateSystem_Arrays[updateStage];
            int arrLen = sysArr.Length;
            for (int curIdx = 0; curIdx < arrLen; curIdx++)
            {
                var curSys = sysArr[curIdx];
                if (curSys == null)
                {
                    // null terminator
                    break;
                }

                curSys.NetworkUpdate(updateStage);
            }
        }

        internal struct NetworkInitialization
        {
            public static PlayerLoopSystem CreateLoopSystem()
            {
                return new PlayerLoopSystem
                {
                    type = typeof(NetworkInitialization),
                    updateDelegate = () => RunNetworkUpdateStage(NetworkUpdateStage.Initialization)
                };
            }
        }

        internal struct NetworkEarlyUpdate
        {
            public static PlayerLoopSystem CreateLoopSystem()
            {
                return new PlayerLoopSystem
                {
                    type = typeof(NetworkEarlyUpdate),
                    updateDelegate = () => RunNetworkUpdateStage(NetworkUpdateStage.EarlyUpdate)
                };
            }
        }

        internal struct NetworkFixedUpdate
        {
            public static PlayerLoopSystem CreateLoopSystem()
            {
                return new PlayerLoopSystem
                {
                    type = typeof(NetworkFixedUpdate),
                    updateDelegate = () => RunNetworkUpdateStage(NetworkUpdateStage.FixedUpdate)
                };
            }
        }

        internal struct NetworkPreUpdate
        {
            public static PlayerLoopSystem CreateLoopSystem()
            {
                return new PlayerLoopSystem
                {
                    type = typeof(NetworkPreUpdate),
                    updateDelegate = () => RunNetworkUpdateStage(NetworkUpdateStage.PreUpdate)
                };
            }
        }

        internal struct NetworkUpdate
        {
            public static PlayerLoopSystem CreateLoopSystem()
            {
                return new PlayerLoopSystem
                {
                    type = typeof(NetworkUpdate),
                    updateDelegate = () => RunNetworkUpdateStage(NetworkUpdateStage.Update)
                };
            }
        }

        internal struct NetworkPreLateUpdate
        {
            public static PlayerLoopSystem CreateLoopSystem()
            {
                return new PlayerLoopSystem
                {
                    type = typeof(NetworkPreLateUpdate),
                    updateDelegate = () => RunNetworkUpdateStage(NetworkUpdateStage.PreLateUpdate)
                };
            }
        }

        internal struct NetworkPostLateUpdate
        {
            public static PlayerLoopSystem CreateLoopSystem()
            {
                return new PlayerLoopSystem
                {
                    type = typeof(NetworkPostLateUpdate),
                    updateDelegate = () => RunNetworkUpdateStage(NetworkUpdateStage.PostLateUpdate)
                };
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            var customPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < customPlayerLoop.subSystemList.Length; i++)
            {
                var playerLoopSystem = customPlayerLoop.subSystemList[i];

                if (playerLoopSystem.type == typeof(Initialization))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        // insert at the bottom of `Initialization`
                        subsystems.Add(NetworkInitialization.CreateLoopSystem());
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(EarlyUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int subsystemCount = subsystems.Count;
                        for (int k = 0; k < subsystemCount; k++)
                        {
                            if (subsystems[k].type == typeof(EarlyUpdate.ScriptRunDelayedStartupFrame))
                            {
                                // insert before `EarlyUpdate.ScriptRunDelayedStartupFrame`
                                subsystems.Insert(k, NetworkEarlyUpdate.CreateLoopSystem());
                                break;
                            }
                        }
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(FixedUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int subsystemCount = subsystems.Count;
                        for (int k = 0; k < subsystemCount; k++)
                        {
                            if (subsystems[k].type == typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate))
                            {
                                // insert before `FixedUpdate.ScriptRunBehaviourFixedUpdate`
                                subsystems.Insert(k, NetworkFixedUpdate.CreateLoopSystem());
                                break;
                            }
                        }
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(PreUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int subsystemCount = subsystems.Count;
                        for (int k = 0; k < subsystemCount; k++)
                        {
                            if (subsystems[k].type == typeof(PreUpdate.PhysicsUpdate))
                            {
                                // insert before `PreUpdate.PhysicsUpdate`
                                subsystems.Insert(k, NetworkPreUpdate.CreateLoopSystem());
                                break;
                            }
                        }
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(Update))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int subsystemCount = subsystems.Count;
                        for (int k = 0; k < subsystemCount; k++)
                        {
                            if (subsystems[k].type == typeof(Update.ScriptRunBehaviourUpdate))
                            {
                                // insert before `Update.ScriptRunBehaviourUpdate`
                                subsystems.Insert(k, NetworkUpdate.CreateLoopSystem());
                                break;
                            }
                        }
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(PreLateUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int subsystemCount = subsystems.Count;
                        for (int k = 0; k < subsystemCount; k++)
                        {
                            if (subsystems[k].type == typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate))
                            {
                                // insert before `PreLateUpdate.ScriptRunBehaviourLateUpdate`
                                subsystems.Insert(k, NetworkPreLateUpdate.CreateLoopSystem());
                                break;
                            }
                        }
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(PostLateUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int subsystemCount = subsystems.Count;
                        for (int k = 0; k < subsystemCount; k++)
                        {
                            if (subsystems[k].type == typeof(PostLateUpdate.PlayerSendFrameComplete))
                            {
                                // insert after `PostLateUpdate.PlayerSendFrameComplete`
                                subsystems.Insert(k + 1, NetworkPostLateUpdate.CreateLoopSystem());
                                break;
                            }
                        }
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }

                customPlayerLoop.subSystemList[i] = playerLoopSystem;
            }

            PlayerLoop.SetPlayerLoop(customPlayerLoop);
        }
    }
}
