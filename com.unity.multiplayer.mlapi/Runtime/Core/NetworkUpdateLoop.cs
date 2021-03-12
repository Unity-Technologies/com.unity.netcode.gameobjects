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

        internal enum PlayerloopAction
        {
            Before,
            After
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            UnRegisterPlayerloopSystems();
            RegisterPlayerloopSystems();
        }

        private static bool CheckAddLoopDelegate(
            ref PlayerLoopSystem system,
            PlayerLoopSystem networkLoopSystem,
            int position,
            Type loopType,
            Type subLoopType = null,
            PlayerloopAction playerloopAction = PlayerloopAction.After)
        {
            if (system.type == loopType)
            {
                if (subLoopType != null)
                {
                    if (system.subSystemList == null)
                        return false;

                    for (int k = 0; k < system.subSystemList.Length; k++)
                    {
                        PlayerLoopSystem subSystem = system.subSystemList[k];
                        if (subSystem.type == subLoopType)
                        {
                            var currentPosition = k;
                            if (playerloopAction == PlayerloopAction.After)
                                currentPosition += 1;

                            if (CheckAddLoopDelegate(ref system, networkLoopSystem, currentPosition, loopType, null, playerloopAction))
                                return true;
                        }
                    }
                }
                else
                {
                    int oldListLength = (system.subSystemList != null) ? system.subSystemList.Length : 0;
                    var newSubsystemList = new PlayerLoopSystem[oldListLength + 1];

                    // Array.Copy is a fast copy much faster than involving a List<T>
                    // or a for loop
                    if (position > 0)
                    {
                        Array.Copy(system.subSystemList, newSubsystemList, position);
                    }

                    if (position < oldListLength)
                    {
                        Array.Copy(system.subSystemList, position, newSubsystemList, position + 1, system.subSystemList.Length - position);
                    }

                    newSubsystemList[position] = networkLoopSystem;
                    system.subSystemList = newSubsystemList;
                   
                    return true;
                }
            }

            return false;
        }

        private static bool CheckRemoveLoopDelegate(
            ref PlayerLoopSystem system,
            Type networkLoopType,
            int position,
            Type loopType)
        {
            if (system.type == loopType)
            {
                if (networkLoopType != null )
                {
                    for (int k = 0; k < system.subSystemList.Length; k++)
                    {

                        PlayerLoopSystem subSystem = system.subSystemList[k];
                        if (subSystem.type == networkLoopType)
                        {
                            var currentPosition = k;

                            if (CheckRemoveLoopDelegate(ref system, null, currentPosition, loopType))
                                return true;
                        }
                    }
                }
                else
                {
                    int oldListLength = (system.subSystemList != null) ? system.subSystemList.Length : 0;
                    var length = 1;
                    if (position + length > oldListLength)
                    {
                        length = oldListLength - position;
                    }

                    var newSubsystemList = new PlayerLoopSystem[oldListLength - length];
                    if (position > 0)
                    {
                        Array.Copy(system.subSystemList, newSubsystemList, position);
                    }

                    if (position < newSubsystemList.Length)
                    {
                        Array.Copy(system.subSystemList, position + length, newSubsystemList, position, newSubsystemList.Length - position);
                    }

                    system.subSystemList = newSubsystemList;

                    return true;
                }
            }

            return false;
        }

        private static void RegisterPlayerloopSystems()
        {
            var rootPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < rootPlayerLoop.subSystemList.Length; i++)
            {
                ref PlayerLoopSystem currentSystem = ref rootPlayerLoop.subSystemList[i];

                CheckAddLoopDelegate(ref currentSystem,
                    NetworkInitialization.CreateLoopSystem(),
                    currentSystem.subSystemList.Length,
                    typeof(Initialization));

                CheckAddLoopDelegate(ref currentSystem,
                    NetworkEarlyUpdate.CreateLoopSystem(),
                    currentSystem.subSystemList.Length,
                    typeof(EarlyUpdate),
                    typeof(EarlyUpdate.ScriptRunDelayedStartupFrame),
                    PlayerloopAction.Before);

                CheckAddLoopDelegate(ref currentSystem,
                    NetworkFixedUpdate.CreateLoopSystem(),
                    currentSystem.subSystemList.Length,
                    typeof(FixedUpdate),
                    typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate),
                    PlayerloopAction.Before);

                CheckAddLoopDelegate(ref currentSystem,
                    NetworkPreUpdate.CreateLoopSystem(),
                    currentSystem.subSystemList.Length,
                    typeof(PreUpdate),
                    typeof(PreUpdate.PhysicsUpdate),
                    PlayerloopAction.Before);

                CheckAddLoopDelegate(ref currentSystem,
                    NetworkUpdate.CreateLoopSystem(),
                    currentSystem.subSystemList.Length,
                    typeof(Update),
                    typeof(Update.ScriptRunBehaviourUpdate),
                    PlayerloopAction.Before);

                CheckAddLoopDelegate(ref currentSystem,
                    NetworkPreLateUpdate.CreateLoopSystem(),
                    currentSystem.subSystemList.Length,
                    typeof(PreLateUpdate),
                    typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate),
                    PlayerloopAction.Before);

                CheckAddLoopDelegate(ref currentSystem,
                    NetworkPostLateUpdate.CreateLoopSystem(),
                    currentSystem.subSystemList.Length,
                    typeof(PostLateUpdate),
                    typeof(PostLateUpdate.PlayerSendFrameComplete),
                    PlayerloopAction.After);
            }

            PlayerLoop.SetPlayerLoop(rootPlayerLoop);
        }

        private static void UnRegisterPlayerloopSystems()
        {
            var rootPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < rootPlayerLoop.subSystemList.Length; i++)
            {
                ref PlayerLoopSystem currentSystem = ref rootPlayerLoop.subSystemList[i];

                CheckRemoveLoopDelegate(ref currentSystem,
                    typeof(NetworkInitialization),
                    i,
                    typeof(Initialization));

                CheckRemoveLoopDelegate(ref currentSystem,
                    typeof(NetworkEarlyUpdate),
                    i,
                    typeof(EarlyUpdate));

                CheckRemoveLoopDelegate(ref currentSystem,
                    typeof(NetworkFixedUpdate),
                    i,
                    typeof(FixedUpdate));

                CheckRemoveLoopDelegate(ref currentSystem,
                    typeof(NetworkPreUpdate),
                    i,
                    typeof(PreUpdate));

                CheckRemoveLoopDelegate(ref currentSystem,
                    typeof(NetworkUpdate),
                    i,
                    typeof(Update));

                CheckRemoveLoopDelegate(ref currentSystem,
                    typeof(NetworkPreLateUpdate),
                    i,
                    typeof(PreLateUpdate));

                CheckRemoveLoopDelegate(ref currentSystem,
                    typeof(NetworkPostLateUpdate),
                    i,
                    typeof(PostLateUpdate));
            }

            PlayerLoop.SetPlayerLoop(rootPlayerLoop);
        }
    }
}
