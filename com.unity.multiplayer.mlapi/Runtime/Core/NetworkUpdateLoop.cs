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

        private struct NetworkInitialization
        {
            public static PlayerLoopSystem CreateLoopSystem()
            {
                return new PlayerLoopSystem
                {
                    type = typeof(NetworkInitialization),
                    updateDelegate = RunNetworkInitialization
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
                    updateDelegate = RunNetworkEarlyUpdate
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
                    updateDelegate = RunNetworkFixedUpdate
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
                    updateDelegate = RunNetworkPreUpdate
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
                    updateDelegate = RunNetworkUpdate
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
                    updateDelegate = RunNetworkPreLateUpdate
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
                    updateDelegate = RunNetworkPostLateUpdate
                };
            }
        }

        /// <summary>
        /// Registers a network update system to be executed in all network update stages.
        /// </summary>
        public static void RegisterAllNetworkUpdates(this INetworkUpdateSystem updateSystem)
        {
            RegisterNetworkUpdate(updateSystem, NetworkUpdateStage.Initialization);
            RegisterNetworkUpdate(updateSystem, NetworkUpdateStage.EarlyUpdate);
            RegisterNetworkUpdate(updateSystem, NetworkUpdateStage.FixedUpdate);
            RegisterNetworkUpdate(updateSystem, NetworkUpdateStage.PreUpdate);
            RegisterNetworkUpdate(updateSystem, NetworkUpdateStage.Update);
            RegisterNetworkUpdate(updateSystem, NetworkUpdateStage.PreLateUpdate);
            RegisterNetworkUpdate(updateSystem, NetworkUpdateStage.PostLateUpdate);
        }

        /// <summary>
        /// Registers a network update system to be executed in a specific network update stage.
        /// </summary>
        public static void RegisterNetworkUpdate(this INetworkUpdateSystem updateSystem, NetworkUpdateStage updateStage = NetworkUpdateStage.Update)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Initialization:
                {
                    if (!m_Initialization_List.Contains(updateSystem))
                    {
                        m_Initialization_List.Add(updateSystem);
                        m_Initialization_Array = m_Initialization_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.EarlyUpdate:
                {
                    if (!m_EarlyUpdate_List.Contains(updateSystem))
                    {
                        m_EarlyUpdate_List.Add(updateSystem);
                        m_EarlyUpdate_Array = m_EarlyUpdate_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.FixedUpdate:
                {
                    if (!m_FixedUpdate_List.Contains(updateSystem))
                    {
                        m_FixedUpdate_List.Add(updateSystem);
                        m_FixedUpdate_Array = m_FixedUpdate_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.PreUpdate:
                {
                    if (!m_PreUpdate_List.Contains(updateSystem))
                    {
                        m_PreUpdate_List.Add(updateSystem);
                        m_PreUpdate_Array = m_PreUpdate_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.Update:
                {
                    if (!m_Update_List.Contains(updateSystem))
                    {
                        m_Update_List.Add(updateSystem);
                        m_Update_Array = m_Update_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.PreLateUpdate:
                {
                    if (!m_PreLateUpdate_List.Contains(updateSystem))
                    {
                        m_PreLateUpdate_List.Add(updateSystem);
                        m_PreLateUpdate_Array = m_PreLateUpdate_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.PostLateUpdate:
                {
                    if (!m_PostLateUpdate_List.Contains(updateSystem))
                    {
                        m_PostLateUpdate_List.Add(updateSystem);
                        m_PostLateUpdate_Array = m_PostLateUpdate_List.ToArray();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Unregisters a network update system from all network update stages.
        /// </summary>
        public static void UnregisterAllNetworkUpdates(this INetworkUpdateSystem updateSystem)
        {
            UnregisterNetworkUpdate(updateSystem, NetworkUpdateStage.Initialization);
            UnregisterNetworkUpdate(updateSystem, NetworkUpdateStage.EarlyUpdate);
            UnregisterNetworkUpdate(updateSystem, NetworkUpdateStage.FixedUpdate);
            UnregisterNetworkUpdate(updateSystem, NetworkUpdateStage.PreUpdate);
            UnregisterNetworkUpdate(updateSystem, NetworkUpdateStage.Update);
            UnregisterNetworkUpdate(updateSystem, NetworkUpdateStage.PreLateUpdate);
            UnregisterNetworkUpdate(updateSystem, NetworkUpdateStage.PostLateUpdate);
        }

        /// <summary>
        /// Unregisters a network update system from a specific network update stage.
        /// </summary>
        public static void UnregisterNetworkUpdate(this INetworkUpdateSystem updateSystem, NetworkUpdateStage updateStage = NetworkUpdateStage.Update)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Initialization:
                {
                    if (m_Initialization_List.Contains(updateSystem))
                    {
                        m_Initialization_List.Remove(updateSystem);
                        m_Initialization_Array = m_Initialization_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.EarlyUpdate:
                {
                    if (m_EarlyUpdate_List.Contains(updateSystem))
                    {
                        m_EarlyUpdate_List.Remove(updateSystem);
                        m_EarlyUpdate_Array = m_EarlyUpdate_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.FixedUpdate:
                {
                    if (m_FixedUpdate_List.Contains(updateSystem))
                    {
                        m_FixedUpdate_List.Remove(updateSystem);
                        m_FixedUpdate_Array = m_FixedUpdate_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.PreUpdate:
                {
                    if (m_PreUpdate_List.Contains(updateSystem))
                    {
                        m_PreUpdate_List.Remove(updateSystem);
                        m_PreUpdate_Array = m_PreUpdate_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.Update:
                {
                    if (m_Update_List.Contains(updateSystem))
                    {
                        m_Update_List.Remove(updateSystem);
                        m_Update_Array = m_Update_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.PreLateUpdate:
                {
                    if (m_PreLateUpdate_List.Contains(updateSystem))
                    {
                        m_PreLateUpdate_List.Remove(updateSystem);
                        m_PreLateUpdate_Array = m_PreLateUpdate_List.ToArray();
                    }

                    break;
                }
                case NetworkUpdateStage.PostLateUpdate:
                {
                    if (m_PostLateUpdate_List.Contains(updateSystem))
                    {
                        m_PostLateUpdate_List.Remove(updateSystem);
                        m_PostLateUpdate_Array = m_PostLateUpdate_List.ToArray();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// The current network update stage being executed.
        /// </summary>
        public static NetworkUpdateStage UpdateStage;

        private static readonly List<INetworkUpdateSystem> m_Initialization_List = new List<INetworkUpdateSystem>();
        private static INetworkUpdateSystem[] m_Initialization_Array = new INetworkUpdateSystem[0];

        private static void RunNetworkInitialization()
        {
            UpdateStage = NetworkUpdateStage.Initialization;
            int arrayLength = m_Initialization_Array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                m_Initialization_Array[i].NetworkUpdate(UpdateStage);
            }
        }

        private static readonly List<INetworkUpdateSystem> m_EarlyUpdate_List = new List<INetworkUpdateSystem>();
        private static INetworkUpdateSystem[] m_EarlyUpdate_Array = new INetworkUpdateSystem[0];

        private static void RunNetworkEarlyUpdate()
        {
            UpdateStage = NetworkUpdateStage.EarlyUpdate;
            int arrayLength = m_EarlyUpdate_Array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                m_EarlyUpdate_Array[i].NetworkUpdate(UpdateStage);
            }
        }

        private static readonly List<INetworkUpdateSystem> m_FixedUpdate_List = new List<INetworkUpdateSystem>();
        private static INetworkUpdateSystem[] m_FixedUpdate_Array = new INetworkUpdateSystem[0];

        private static void RunNetworkFixedUpdate()
        {
            UpdateStage = NetworkUpdateStage.FixedUpdate;
            int arrayLength = m_FixedUpdate_Array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                m_FixedUpdate_Array[i].NetworkUpdate(UpdateStage);
            }
        }

        private static readonly List<INetworkUpdateSystem> m_PreUpdate_List = new List<INetworkUpdateSystem>();
        private static INetworkUpdateSystem[] m_PreUpdate_Array = new INetworkUpdateSystem[0];

        private static void RunNetworkPreUpdate()
        {
            UpdateStage = NetworkUpdateStage.PreUpdate;
            int arrayLength = m_PreUpdate_Array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                m_PreUpdate_Array[i].NetworkUpdate(UpdateStage);
            }
        }

        private static readonly List<INetworkUpdateSystem> m_Update_List = new List<INetworkUpdateSystem>();
        private static INetworkUpdateSystem[] m_Update_Array = new INetworkUpdateSystem[0];

        private static void RunNetworkUpdate()
        {
            UpdateStage = NetworkUpdateStage.Update;
            int arrayLength = m_Update_Array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                m_Update_Array[i].NetworkUpdate(UpdateStage);
            }
        }

        private static readonly List<INetworkUpdateSystem> m_PreLateUpdate_List = new List<INetworkUpdateSystem>();
        private static INetworkUpdateSystem[] m_PreLateUpdate_Array = new INetworkUpdateSystem[0];

        private static void RunNetworkPreLateUpdate()
        {
            UpdateStage = NetworkUpdateStage.PreLateUpdate;
            int arrayLength = m_PreLateUpdate_Array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                m_PreLateUpdate_Array[i].NetworkUpdate(UpdateStage);
            }
        }

        private static readonly List<INetworkUpdateSystem> m_PostLateUpdate_List = new List<INetworkUpdateSystem>();
        private static INetworkUpdateSystem[] m_PostLateUpdate_Array = new INetworkUpdateSystem[0];

        private static void RunNetworkPostLateUpdate()
        {
            UpdateStage = NetworkUpdateStage.PostLateUpdate;
            int arrayLength = m_PostLateUpdate_Array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                m_PostLateUpdate_Array[i].NetworkUpdate(UpdateStage);
            }
        }
    }
}