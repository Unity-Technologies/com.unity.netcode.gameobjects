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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            UnRegisterPlayerloopSystems();
            RegisterPlayerloopSystems();
        }

        private static bool CheckAddLoopDelegate(ref PlayerLoopSystem system, PlayerLoopSystem.UpdateFunction action, Type loopType, Type subLoopType = null)
        {
            if (system.type == loopType)
            {
                if (subLoopType != null)
                {
                    for (int k = 0; k < system.subSystemList.Length; k++)
                    {
                        if (CheckAddLoopDelegate(ref system.subSystemList[k], action, subLoopType))
                            return true;
                    }
                }
                else
                {
                    system.updateDelegate += action;
                    return true;

                }
            }

            return false;
        }

        private static bool CheckRemoveLoopDelegate(ref PlayerLoopSystem system, PlayerLoopSystem.UpdateFunction action, Type loopType, Type subLoopType = null)
        {
            if (system.type == loopType)
            {
                if (subLoopType != null)
                {
                    for (int k = 0; k < system.subSystemList.Length; k++)
                    {
                        if (CheckRemoveLoopDelegate(ref system.subSystemList[k], action, subLoopType))
                            return true;
                    }
                }
                else
                {
                    system.updateDelegate -= action;
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
                CheckAddLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.Initialization),
                    typeof(Initialization));

                CheckAddLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.EarlyUpdate),
                    typeof(EarlyUpdate),
                    typeof(EarlyUpdate.ScriptRunDelayedStartupFrame));

                CheckAddLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.FixedUpdate),
                    typeof(FixedUpdate),
                    typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate));

                CheckAddLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.PreUpdate),
                    typeof(PreUpdate),
                    typeof(PreUpdate.PhysicsUpdate));

                CheckAddLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.Update),
                    typeof(Update),
                    typeof(Update.ScriptRunBehaviourUpdate));

                CheckAddLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.PreLateUpdate),
                    typeof(PreLateUpdate),
                    typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate));

                CheckAddLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.PostLateUpdate),
                    typeof(PostLateUpdate),
                    typeof(PostLateUpdate.PlayerSendFrameComplete));
            }

            PlayerLoop.SetPlayerLoop(rootPlayerLoop);
        }

        private static void UnRegisterPlayerloopSystems()
        {
            var rootPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < rootPlayerLoop.subSystemList.Length; i++)
            {
                CheckRemoveLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.Initialization),
                    typeof(Initialization));

                CheckRemoveLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.EarlyUpdate),
                    typeof(EarlyUpdate),
                    typeof(EarlyUpdate.ScriptRunDelayedStartupFrame));

                CheckRemoveLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.FixedUpdate),
                    typeof(FixedUpdate),
                    typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate));

                CheckRemoveLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.PreUpdate),
                    typeof(PreUpdate),
                    typeof(PreUpdate.PhysicsUpdate));

                CheckRemoveLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.Update),
                    typeof(Update),
                    typeof(Update.ScriptRunBehaviourUpdate));

                CheckRemoveLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.PreLateUpdate),
                    typeof(PreLateUpdate),
                    typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate));

                CheckRemoveLoopDelegate(ref rootPlayerLoop.subSystemList[i],
                    () => RunNetworkUpdateStage(NetworkUpdateStage.PostLateUpdate),
                    typeof(PostLateUpdate),
                    typeof(PostLateUpdate.PlayerSendFrameComplete));
            }

            PlayerLoop.SetPlayerLoop(rootPlayerLoop);
        }

    }
}
