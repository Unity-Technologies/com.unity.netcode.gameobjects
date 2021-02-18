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
                        int indexOfScriptRunDelayedStartupFrame = subsystems.FindIndex(s => s.type == typeof(EarlyUpdate.ScriptRunDelayedStartupFrame));
                        if (indexOfScriptRunDelayedStartupFrame < 0)
                        {
                            Debug.LogError($"{nameof(NetworkUpdateLoop)}.{nameof(Initialize)}: Cannot find index of `{nameof(EarlyUpdate.ScriptRunDelayedStartupFrame)}` loop system in `{nameof(EarlyUpdate)}`'s subsystem list!");
                            return;
                        }

                        // insert before `EarlyUpdate.ScriptRunDelayedStartupFrame`
                        subsystems.Insert(indexOfScriptRunDelayedStartupFrame, NetworkEarlyUpdate.CreateLoopSystem());
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(FixedUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int indexOfScriptRunBehaviourFixedUpdate = subsystems.FindIndex(s => s.type == typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate));
                        if (indexOfScriptRunBehaviourFixedUpdate < 0)
                        {
                            Debug.LogError($"{nameof(NetworkUpdateLoop)}.{nameof(Initialize)}: Cannot find index of `{nameof(FixedUpdate.ScriptRunBehaviourFixedUpdate)}` loop system in `{nameof(FixedUpdate)}`'s subsystem list!");
                            return;
                        }

                        // insert before `FixedUpdate.ScriptRunBehaviourFixedUpdate`
                        subsystems.Insert(indexOfScriptRunBehaviourFixedUpdate, NetworkFixedUpdate.CreateLoopSystem());
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(PreUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int indexOfPhysicsUpdate = subsystems.FindIndex(s => s.type == typeof(PreUpdate.PhysicsUpdate));
                        if (indexOfPhysicsUpdate < 0)
                        {
                            Debug.LogError($"{nameof(NetworkUpdateLoop)}.{nameof(Initialize)}: Cannot find index of `{nameof(PreUpdate.PhysicsUpdate)}` loop system in `{nameof(PreUpdate)}`'s subsystem list!");
                            return;
                        }

                        // insert before `PreUpdate.PhysicsUpdate`
                        subsystems.Insert(indexOfPhysicsUpdate, NetworkPreUpdate.CreateLoopSystem());
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(Update))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int indexOfScriptRunBehaviourUpdate = subsystems.FindIndex(s => s.type == typeof(Update.ScriptRunBehaviourUpdate));
                        if (indexOfScriptRunBehaviourUpdate < 0)
                        {
                            Debug.LogError($"{nameof(NetworkUpdateLoop)}.{nameof(Initialize)}: Cannot find index of `{nameof(Update.ScriptRunBehaviourUpdate)}` loop system in `{nameof(Update)}`'s subsystem list!");
                            return;
                        }

                        // insert before `Update.ScriptRunBehaviourUpdate`
                        subsystems.Insert(indexOfScriptRunBehaviourUpdate, NetworkUpdate.CreateLoopSystem());
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(PreLateUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int indexOfScriptRunBehaviourLateUpdate = subsystems.FindIndex(s => s.type == typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate));
                        if (indexOfScriptRunBehaviourLateUpdate < 0)
                        {
                            Debug.LogError($"{nameof(NetworkUpdateLoop)}.{nameof(Initialize)}: Cannot find index of `{nameof(PreLateUpdate.ScriptRunBehaviourLateUpdate)}` loop system in `{nameof(PreLateUpdate)}`'s subsystem list!");
                            return;
                        }

                        // insert before `PreLateUpdate.ScriptRunBehaviourLateUpdate`
                        subsystems.Insert(indexOfScriptRunBehaviourLateUpdate, NetworkPreLateUpdate.CreateLoopSystem());
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }
                else if (playerLoopSystem.type == typeof(PostLateUpdate))
                {
                    var subsystems = playerLoopSystem.subSystemList.ToList();
                    {
                        int indexOfPlayerSendFrameComplete = subsystems.FindIndex(s => s.type == typeof(PostLateUpdate.PlayerSendFrameComplete));
                        if (indexOfPlayerSendFrameComplete < 0)
                        {
                            Debug.LogError($"{nameof(NetworkUpdateLoop)}.{nameof(Initialize)}: Cannot find index of `{nameof(PostLateUpdate.PlayerSendFrameComplete)}` loop system in `{nameof(PostLateUpdate)}`'s subsystem list!");
                            return;
                        }

                        // insert after `PostLateUpdate.PlayerSendFrameComplete`
                        subsystems.Insert(indexOfPlayerSendFrameComplete + 1, NetworkPostLateUpdate.CreateLoopSystem());
                    }
                    playerLoopSystem.subSystemList = subsystems.ToArray();
                }

                customPlayerLoop.subSystemList[i] = playerLoopSystem;
            }

            PlayerLoop.SetPlayerLoop(customPlayerLoop);
        }

        static NetworkUpdateLoop()
        {
            m_UpdateSystem_Sets = new Dictionary<NetworkUpdateStage, HashSet<INetworkUpdateSystem>>();
            m_UpdateSystem_Arrays = new Dictionary<NetworkUpdateStage, INetworkUpdateSystem[]>();

            foreach (NetworkUpdateStage updateStage in Enum.GetValues(typeof(NetworkUpdateStage)))
            {
                m_UpdateSystem_Sets.Add(updateStage, new HashSet<INetworkUpdateSystem>());
                m_UpdateSystem_Arrays.Add(updateStage, new INetworkUpdateSystem[0]);
            }
        }

        private static readonly Dictionary<NetworkUpdateStage, HashSet<INetworkUpdateSystem>> m_UpdateSystem_Sets;
        private static readonly Dictionary<NetworkUpdateStage, INetworkUpdateSystem[]> m_UpdateSystem_Arrays;

        /// <summary>
        /// The current network update stage being executed.
        /// </summary>
        public static NetworkUpdateStage UpdateStage;

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
            if (!m_UpdateSystem_Sets[updateStage].Contains(updateSystem))
            {
                m_UpdateSystem_Sets[updateStage].Add(updateSystem);
                m_UpdateSystem_Arrays[updateStage] = m_UpdateSystem_Sets[updateStage].ToArray();
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
            if (m_UpdateSystem_Sets[updateStage].Contains(updateSystem))
            {
                m_UpdateSystem_Sets[updateStage].Remove(updateSystem);
                m_UpdateSystem_Arrays[updateStage] = m_UpdateSystem_Sets[updateStage].ToArray();
            }
        }

        private static void RunNetworkUpdateStage(NetworkUpdateStage updateStage)
        {
            UpdateStage = updateStage;
            var updateSystems = m_UpdateSystem_Arrays[updateStage];
            int arrayLength = updateSystems.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                updateSystems[i].NetworkUpdate(updateStage);
            }
        }

        private struct NetworkInitialization
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

        private struct NetworkEarlyUpdate
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

        private struct NetworkFixedUpdate
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

        private struct NetworkPreUpdate
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

        private struct NetworkUpdate
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

        private struct NetworkPreLateUpdate
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

        private struct NetworkPostLateUpdate
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
    }
}