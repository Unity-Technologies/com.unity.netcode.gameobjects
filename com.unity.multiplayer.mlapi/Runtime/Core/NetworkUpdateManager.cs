/// About the Network Update Loop
/// The NetworkUpdateEngine is a temporary solution for the network update loop implementation.
/// This will be revised with a more robust and modular implementation in the near future.

using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.LowLevel;

namespace MLAPI
{
    /// <summary>
    /// NetworkUpdateManager
    /// External public facing class for the registration of and processing of network updates
    /// </summary>
    public class NetworkUpdateManager
    {
        public enum NetworkUpdateStage
        {
            Default, //Will default to the UPDATE stage if no setting was made
            PreUpdate, //Invoked after EarlyUpdate.UnityWebRequestUpdate
            FixedUpdate, //Invoked after FixedUpdate.AudioFixedUpdate (prior to any physics being applied or simulated)
            Update, //Invoked after PreUpdate.UpdateVideo   (just before the primary Update is invoked)
            LateUpdate //Invoked after PostLateUpdate.ProcessWebSendMessages (after all updates)
        }

        private static Dictionary<INetworkUpdateLoopSystem, Dictionary<NetworkUpdateStage, PlayerLoopSystem>> s_RegisteredUpdateLoopSystems = new Dictionary<INetworkUpdateLoopSystem, Dictionary<NetworkUpdateStage, PlayerLoopSystem>>();


        /// <summary>
        /// RegisterSystem
        /// Registers all of the defined update stages
        /// </summary>
        /// <param name="systems"></param>
        private static void RegisterSystem(Dictionary<NetworkUpdateStage, PlayerLoopSystem> systems)
        {
            var def = PlayerLoop.GetCurrentPlayerLoop();

            foreach (var updateStage in systems)
            {
                var InsertAfterType = typeof(EarlyUpdate.UnityWebRequestUpdate);
                switch (updateStage.Key)
                {
                    case NetworkUpdateStage.PreUpdate:
                    {
                        InsertAfterType = typeof(EarlyUpdate.UnityWebRequestUpdate);
                        break;
                    }
                    case NetworkUpdateStage.FixedUpdate:
                    {
                        InsertAfterType = typeof(FixedUpdate.AudioFixedUpdate);
                        break;
                    }
                    case NetworkUpdateStage.Update:
                    {
                        InsertAfterType = typeof(PreUpdate.UpdateVideo);
                        break;
                    }
                    case NetworkUpdateStage.LateUpdate:
                    {
                        InsertAfterType = typeof(PostLateUpdate.ProcessWebSendMessages);
                        break;
                    }
                }

                InsertSystem(ref def, updateStage.Value, InsertAfterType);
            }

#if UNITY_EDITOR
            PrintPlayerLoop(def);
#endif

            PlayerLoop.SetPlayerLoop(def);
        }

        /// <summary>
        /// OnNetworkLoopSystemDestroyed
        /// This should be invoked by the registered INetworkUpdateLoopSystem when the class is being destroyed or it no longer wants to receive updates
        /// </summary>
        /// <param name="networkLoopSystem"></param>
        private static void OnNetworkLoopSystemDestroyed(INetworkUpdateLoopSystem networkLoopSystem)
        {
            if (s_RegisteredUpdateLoopSystems.ContainsKey(networkLoopSystem))
            {
                var def = PlayerLoop.GetCurrentPlayerLoop();
                foreach (KeyValuePair<NetworkUpdateStage, PlayerLoopSystem> updateStage in s_RegisteredUpdateLoopSystems[networkLoopSystem])
                {
                    if (!RemoveSystem(ref def, updateStage.Value))
                    {
                        Debug.LogWarning(updateStage.Value.type.Name + " tried to remove itself but no instsance was found!!!");
                    }
                }

                s_RegisteredUpdateLoopSystems.Remove(networkLoopSystem);
#if UNITY_EDITOR
                PrintPlayerLoop(def);
#endif
                PlayerLoop.SetPlayerLoop(def);
            }
        }

        /// <summary>
        /// NetworkLoopRegistration
        /// This will register any class that has an INetworkUpdateLoopSystem interface assignment
        /// </summary>
        /// <param name="networkLoopSystem">class instace to register</param>
        public static void NetworkLoopRegistration(INetworkUpdateLoopSystem networkLoopSystem)
        {
            if (!s_RegisteredUpdateLoopSystems.ContainsKey(networkLoopSystem))
            {
                Dictionary<NetworkUpdateStage, PlayerLoopSystem> RegisterPlayerLoopSystems = new Dictionary<NetworkUpdateStage, PlayerLoopSystem>();

                foreach (NetworkUpdateStage stage in Enum.GetValues(typeof(NetworkUpdateStage)))
                {
                    Action updateFunction = networkLoopSystem.RegisterUpdate(stage);
                    if (updateFunction != null)
                    {
                        PlayerLoopSystem.UpdateFunction callback = new PlayerLoopSystem.UpdateFunction(updateFunction);
                        PlayerLoopSystem stageLoop = new PlayerLoopSystem() { updateDelegate = callback, type = networkLoopSystem.GetType() };
                        if (stageLoop.updateDelegate != null)
                        {
                            RegisterPlayerLoopSystems.Add(stage, stageLoop);
                        }
                    }
                }

                if (RegisterPlayerLoopSystems.Count > 0)
                {
                    //Keep track of which systems are registered.
                    s_RegisteredUpdateLoopSystems.Add(networkLoopSystem, RegisterPlayerLoopSystems);

                    //Actually register all valid update stages for this system
                    RegisterSystem(RegisterPlayerLoopSystems);

                    //Register the callback to be used when the system is removed/deleted/destroyed
                    networkLoopSystem.RegisterUpdateLoopSystemDestroyCallback(OnNetworkLoopSystemDestroyed);
                }
            }
        }

        /// <summary>
        /// RemoveSystem
        /// Recursively search for the given system type and remove system
        /// </summary>
        /// <param name="system"></param>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        private static bool RemoveSystem(ref PlayerLoopSystem system, PlayerLoopSystem toRemove)
        {
            if (system.subSystemList == null)
            {
                return false;
            }

            for (int i = 0; i < system.subSystemList.Length; ++i)
            {
                if (system.subSystemList[i].type == toRemove.type)
                {
                    RemoveSystemAt(ref system, toRemove);
                    return true;
                }
            }

            for (var i = 0; i < system.subSystemList.Length; i++)
            {
                if (RemoveSystem(ref system.subSystemList[i], toRemove))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// RemoveSystemAt
        /// Copies the subSystemList of the given system, but excludes the specified PlayerLoopSystem from the list
        /// </summary>
        /// <param name="system">PlayerLoopSystem to be inserted into</param>
        /// <param name="toRemove">PlayerLoopSystem to insert</param>
        private static void RemoveSystemAt(ref PlayerLoopSystem system, PlayerLoopSystem toRemove)
        {
            PlayerLoopSystem[] newSubSystems = new PlayerLoopSystem[system.subSystemList.Length - 1];
            for (int i = 0, newSystemIdx = 0; i < newSubSystems.Length - 1; ++i)
            {
                if (system.subSystemList[i].type != toRemove.type && system.subSystemList[i].updateFunction != toRemove.updateFunction)
                {
                    newSubSystems[newSystemIdx++] = system.subSystemList[i];
                }
            }

            system.subSystemList = newSubSystems;
        }

        /// <summary>
        /// InsertSystem
        /// Recursively search for the given system type and insert the new system immediately afterwards
        /// </summary>
        /// <param name="system">PlayerLoopSystem to search</param>
        /// <param name="toInsert">PlayerLoopSystem to insert</param>
        /// <param name="insertAfter">location to insert the PlayerLoopSystem</param>
        /// <returns></returns>
        private static bool InsertSystem(ref PlayerLoopSystem system, PlayerLoopSystem toInsert, Type insertAfter)
        {
            if (system.subSystemList == null)
            {
                return false;
            }

            for (int i = 0; i < system.subSystemList.Length; ++i)
            {
                if (system.subSystemList[i].type == insertAfter)
                {
                    InsertSystemAt(ref system, toInsert, i + 1);
                    return true;
                }
            }

            for (var i = 0; i < system.subSystemList.Length; i++)
            {
                if (InsertSystem(ref system.subSystemList[i], toInsert, insertAfter))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// InsertSystemAt
        /// Copies the subSystemList of the given system and inserts a new system at the given index
        /// </summary>
        /// <param name="system">PlayerLoopSystem to be inserted into</param>
        /// <param name="toInsert">PlayerLoopSystem to insert</param>
        /// <param name="pos">position to insert the PlayerLoopSystem</param>
        private static void InsertSystemAt(ref PlayerLoopSystem system, PlayerLoopSystem toInsert, int pos)
        {
            PlayerLoopSystem[] newSubSystems = new PlayerLoopSystem[system.subSystemList.Length + 1];
            for (int i = 0, oldSystemIdx = 0; i < newSubSystems.Length; ++i)
            {
                if (i == pos)
                {
                    newSubSystems[i] = toInsert;
                }
                else
                {
                    newSubSystems[i] = system.subSystemList[oldSystemIdx++];
                }
            }

            system.subSystemList = newSubSystems;
        }

#if UNITY_EDITOR
        /// <summary>
        /// PrintPlayerLoop
        /// Prints all PlayerLoopSystems within the PlayerLoopSystem provided
        /// </summary>
        /// <param name="pl">PlayerLoopSystem</param>
        private static void PrintPlayerLoop(PlayerLoopSystem pl)
        {
            var sb = new StringBuilder();
            RecursivePlayerLoopPrint(pl, sb, 0);
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// RecursivePlayerLoopPrint
        /// Recursively build the entire PlayerLoopSystem list
        /// </summary>
        /// <param name="def">PlayerLoopSystem to be added</param>
        /// <param name="sb">StringBuilder to add to</param>
        /// <param name="depth">Maximum recursion depth</param>
        private static void RecursivePlayerLoopPrint(PlayerLoopSystem def, StringBuilder sb, int depth)
        {
            if (depth == 0)
            {
                sb.AppendLine("ROOT NODE");
            }
            else if (def.type != null)
            {
                for (int i = 0; i < depth; i++)
                {
                    sb.Append("\t");
                }

                sb.AppendLine(def.type.Name);
            }

            if (def.subSystemList != null)
            {
                depth++;
                foreach (var s in def.subSystemList)
                {
                    RecursivePlayerLoopPrint(s, sb, depth);
                }

                depth--;
            }
        }
#endif
    }
}